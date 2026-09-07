# best-stories-api

[![build](https://github.com/DrMuss/best-stories-api/actions/workflows/build.yml/badge.svg)](https://github.com/DrMuss/best-stories-api/actions/workflows/build.yml)

An ASP.NET Core service returning the best _n_ Hacker News stories by score.

Design decisions, and the dependencies deliberately not taken, are recorded in [DESIGN.md](DESIGN.md).

## How it works

A background loop fetches the best-stories list and every item behind it, ranks them by score,
and publishes the result as an immutable snapshot. Requests read that snapshot and never call
Hacker News, so a thousand concurrent callers cost the same upstream traffic as none. The
snapshot is replaced by reference rather than mutated, so a reader never sees a half-built list
and the read path needs no lock. A refresh that fails is discarded whole and the previous
snapshot stands, so upstream trouble degrades freshness rather than availability. Cycles are
skipped while nobody is asking, so upstream load is proportional to use rather than to elapsed
time.

Organised as folders within a single API project rather than a Domain/Application/Infrastructure
split. On a multi-feature API I use feature folders, each with its own DTOs, validators and
handlers. With one endpoint, that structure adds navigation cost without adding separation.

## Running it

```bash
dotnet run --project api/BestStories.Api   # http://localhost:5128
docker compose up                          # http://localhost:8080
```

```bash
curl 'http://localhost:5128/stories?n=10'   # the ten highest-scoring stories
```

In Development the root redirects to the Scalar API reference, where the endpoint can be
exercised from the browser. The OpenAPI document itself is at `/openapi/v1.json`.

`GET /health` is liveness — the process is running. `GET /health/ready` is readiness — this
instance holds a snapshot and can answer. An orchestrator should gate traffic on the second and
restart on the first, and should not point a liveness probe at the readiness endpoint: a cold
instance is starting normally, and restarting it only starts the wait again.

## Configuration

Everything tunable lives in one section of `appsettings.json`, and every value is validated at
startup — a bad setting is a startup failure naming the setting, not a surprise at runtime.

| Setting | Default | What it trades |
|---|---|---|
| `BaseUrl` | `https://hacker-news.firebaseio.com/v0/` | The `v0` is a version pin on someone else's contract. The trailing slash is load-bearing |
| `RefreshInterval` | 1 minute | Freshness against upstream load, in direct proportion. Five minutes would be equally defensible and cost a fifth as many calls |
| `MaxConcurrentItemFetches` | 10 | Cold-start time against politeness to a free API. See the table above |
| `IdleTimeout` | 10 minutes | How long an unused instance keeps refreshing before it stops asking |
| `UpstreamAttemptTimeout` | 10 seconds | How long one attempt waits before being abandoned and retried |
| `RetryDelay` | 2 seconds | The wait before a retry, with jitter applied on top |

Any of them can be overridden per environment in the usual ways, for example
`HackerNews__RefreshInterval=00:05:00` as an environment variable.

## Idle behaviour

Refreshing is proportional to use, not to elapsed time. If no request has been served for
`HackerNews:IdleTimeout` (10 minutes by default), refresh cycles are skipped until someone asks
again — otherwise an instance nobody is using would call Hacker News roughly 200 times a minute
indefinitely. Health probes deliberately do not count as use; an orchestrator polling every few
seconds would otherwise keep every idle instance refreshing forever.

The trade-off is on the caller: the first request after a long idle period is answered
immediately from the snapshot as it was when refreshing stopped, so it can be up to the idle
period plus one interval out of date. That request restarts refreshing, so the next one is
fresh. Serving a stale answer at once was preferred to making one unlucky caller wait for a
fetch, which is the same reasoning as the cold-start 503.

## Assumptions

**`beststories.json` does not return a fixed number of IDs, and never promised to.** The
Hacker News docs give a count for the other lists — "up to 500" for `topstories` and
`newstories`, "up to 200" for `askstories`, `showstories` and `jobstories` — but state no
count for `beststories`. The familiar 500 is inherited from `topstories`.

Sampled on 2026-09-07, within a minute of each other:

| List | Documented | Returned |
|---|---|---|
| `beststories` | not stated | **200** |
| `topstories` | up to 500 | 500 |
| `newstories` | up to 500 | 500 |
| `askstories` / `showstories` / `jobstories` | up to 200 | 66 / 121 / 31 |

So this is particular to `beststories` rather than a change to the whole API, and even the
documented lists return fewer than their cap. The code therefore takes the length of
whatever comes back and hard-codes no number anywhere. An `n` larger than the list is
clamped to what exists and answered `200 OK`, not rejected: the list length is a ceiling
upstream sets, not a mistake the caller made.

**The list happens to arrive in score order, and the code sorts anyway.** Across all 200 items
sampled the scores were monotonically decreasing, from 1270 down to 9 — so trusting the order
would have worked on the day. Hacker News does not document it, so the snapshot is sorted by
score explicitly, with ties broken by id so the same set of stories always serialises
identically. Ties are not rare: 98 of the 200 shared a score with another story.

That costs effectively nothing, because it is not on the request path. Ranking happens once per
refresh, and ranking all 200 items — filtering, sorting with the tie-break, and mapping to the
response shape — takes a median of **20 µs** on the dev machine, 51 µs for 500 items. Against a
60-second refresh interval that is twenty microseconds in sixty million: around 0.00003% of the
cycle. Not trusting an undocumented ordering is free.

**Other findings from the same sample**, each of which the mapping handles: `url` was absent on
8 of 200 items, which is the Ask HN case; `descendants` was present on all 200, so the
absent-or-null case is handled defensively rather than because it was observed; every item was
`type: "story"`, so the filter for jobs and polls is also defensive; and no title carried HTML
entities, so titles are passed through unmodified. An id Hacker News no longer has returns
`200` with a body of `null` rather than a `404` — verified, and the reason a null item is
skipped rather than treated as a failed fetch.

## Cold start

The service starts serving immediately and builds its first snapshot in the background. Until
that snapshot exists, `GET /stories` answers **503** with a `Retry-After` header rather than an
empty array, because "no stories" and "not asked yet" are different answers. `GET /health/ready`
reports not-ready over the same window; `GET /health` stays healthy throughout, since the
process is up and a restart would not help.

How long that window lasts is one full refresh. Measured on the dev machine against the live
Hacker News API (Release build, 200 stories, timed from launch to the first `200` from
`/stories`, so .NET startup is included):

| `MaxConcurrentItemFetches` | Cold start |
|---|---|
| 5 | 5.9s |
| **10 (default)** | **3.5-4.1s** |
| 20 | 3.3s |

Ten is near the knee for a steady-state cap: halving it costs about two seconds, doubling it
buys about half of one. The default is chosen for politeness towards a free, unauthenticated
API rather than for throughput, and the figures are from an Apple-silicon dev machine — a CI
runner will be slower.

Raising the cap for the first refresh only would cut this further: 50 gets to ~1.9s and 100 to
~1.7s, where .NET's own startup dominates. That option is deliberately not taken — see the
omissions table in [DESIGN.md](DESIGN.md). It would save around 1.6 seconds once per process
start, in exchange for a second code path and a burst of connections against a free API at the
moment we know least about its health.

## Tests

```bash
dotnet test                                # whole solution
dotnet test --filter "Category!=Network"   # as CI runs it, excluding live Hacker News
```

To run them as CI actually runs them — Linux, Release, and two cores, which is where timing
assumptions a fast development machine hides tend to surface:

```bash
docker run --rm --cpus 2 -v "$PWD":/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 \
  bash -c 'dotnet test --configuration Release --filter "Category!=Network"'
```

### What is tested where

**Unit tests** cover the pure functions, where a wrong answer is a wrong answer regardless of
plumbing: ranking and the tie-break, the item mapping, the `n` parsing rules, the snapshot's
publish semantics, the options validator, and the refresh loop driven by a fake clock.

**Integration tests** run the whole service in memory through `WebApplicationFactory`, with
Hacker News replaced by a hand-written `HttpMessageHandler` — about a hundred lines that
programs a response per URL, counts calls, records peak concurrency, and can hold responses
open or fail them on demand. That last part is what makes the interesting tests possible:

- `ServingRequests_DoesNotCallHackerNews` — a warm snapshot, a hundred concurrent requests, and
  an assertion that the upstream call count did not move. This is the brief's central
  requirement written as an assertion rather than an intention.
- `Refresh_LimitsConcurrentUpstreamCalls` — holds every item response open and asserts the peak
  in flight equals the configured cap.
- The failure taxonomy: an item that keeps failing discards the whole cycle; a `null`, deleted
  or dead item is skipped and the cycle still publishes; malformed JSON and a too-slow upstream
  discard the cycle; and the endpoint keeps serving the last good snapshot throughout.
- The cold edge: `503` with `Retry-After` before the first snapshot, readiness not-ready over
  the same window, liveness healthy throughout.

**One test hits the real API.** `HackerNewsClient_MatchesLiveContract` is tagged
`[Trait("Category", "Network")]` and excluded from CI, so the field mapping is checked against
reality without a bad day upstream turning the build red.

**Mutation testing** rather than coverage alone: coverage proves a line ran, mutation proves a
test would have failed if the line were wrong. In a real deployment this would run nightly
rather than per commit.

## Coverage, complexity and mutation testing

Tools are pinned in `.config/dotnet-tools.json`:

```bash
dotnet tool restore

dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
dotnet reportgenerator -reports:"TestResults/*/coverage.cobertura.xml" \
  -targetdir:"CoverageReport" -reporttypes:"Html;TextSummary"

dotnet stryker
```

The coverage report's Risk Hotspots page carries cyclomatic complexity and CRAP scores.

### Coverage and complexity

Run on the current commit, excluding the network-tagged test as CI does:

| | |
|---|---|
| Line coverage, hand-written classes | **100%** (all 17) |
| Line coverage, whole assembly | 50.7% |
| Highest cyclomatic complexity | 20, `HackerNewsOptionsValidator.Validate` |
| Methods above complexity 5 | 3 of 50 |
| Highest CRAP score | 20 |

Two of those numbers need their context. The assembly-wide 50.7% is not a gap in the tests:
every class in the project reports 100%, and the aggregate is dragged down by two generated
types the coverage tool counts and nothing calls — the OpenAPI source generator's output, which
exists because XML documentation is enabled, and compiler-generated helpers.

And every CRAP score here equals its method's cyclomatic complexity, because CRAP is
`complexity² × (1 − coverage)³ + complexity` — with full coverage the first term is zero and the
metric collapses to complexity. So CRAP earns its keep as a hotspot finder only where coverage
is partial; here it is complexity with extra steps, and the number worth watching is the 20 on
the options validator.

That 20 is deliberately left alone. Cyclomatic complexity counts branches without regard to
their shape, and this method is a flat run of independent `if` checks — one per setting, no
nesting, no interaction between them — which reads as a list of rules rather than as a
tangle. Splitting it into a validator per setting would lower the number and raise the cost of
finding out what is validated. It is worth revisiting if the rules ever start depending on each
other, since that is when the number would start describing something real.

### Mutation score

**83.0%** — 93 of 110 mutants killed, run locally on the current commit.

The first run scored 68.8%, below this repo's own break threshold, and the survivors were worth
reading rather than explaining away. They pointed at untested boundaries: nothing exercised a
`RefreshInterval` of exactly one second or exactly one day, or a concurrency cap of exactly 1 or
100, so a mutant flipping `<` to `<=` in the validator went unnoticed. Nothing covered Hacker
News answering the *list* endpoint with `null` rather than an array, which would have been a
null-reference at runtime. And several strings that callers actually see went unasserted — the
503's `detail`, the operation id and summary in the OpenAPI document, the `int32` format on `n`,
and the empty `BaseUrl` default that is what makes an unconfigured section fail at startup.

The 17 that survive are deliberate. Most are the *wording* of validation messages: the tests
assert that a failure names the setting that is wrong, not how the sentence reads, because
asserting the exact text against the same source it came from proves nothing and breaks whenever
the wording improves. The rest are log messages and the two health-check descriptions, which
never leave the process, plus one boundary in the idle check that would need a test landing the
fake clock exactly on the timeout.

## Given more time

Ordered by what I think each is worth, not by effort.

1. **Survive a restart during an upstream outage.** The snapshot lives only in memory, so a
   process restart while Hacker News is unavailable leaves the service with nothing to serve
   and no way to rebuild. Writing each good snapshot to disk and loading it at startup would
   turn the worst case from "returns 503 until upstream returns" into "serves stale stories".
   This is the largest real gap in the design.
2. **Telemetry on the refresh path.** Cycle duration, failures, and the age of the current
   snapshot as metrics, so staleness is observable rather than inferred. Right now a failing
   refresh is a log line, and the only external signal is that scores stop moving.
3. **Delta refresh via `updates.json`.** Poll the changed-item feed and re-fetch only the
   intersection with the current best list, with a periodic full reconciliation for
   correctness. Worth doing only with a measured overlap rate behind it — the feed is a
   firehose across all of Hacker News, so the saving is an empirical question, not a
   given.
4. **An `X-Available-Count` response header.** The brief fixes the body as an array, so a
   caller cannot currently tell "exactly the 200 you asked for" from "clamped to the 200 that
   exist". A header answers that without touching the contract.
5. **Inbound rate limiting.** Once the snapshot is warm, inbound load never reaches Hacker
   News, so this protects our own CPU rather than upstream — a different requirement from the
   one set, and the reason it is not built.
6. **Scale-out.** Each instance keeps its own snapshot and its own refresh loop, so ten
   instances make ten times the upstream calls. A shared cache, or one refresher publishing for
   many readers, is the next design conversation rather than a code change.
