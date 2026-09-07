# best-stories-api

[![build](https://github.com/DrMuss/best-stories-api/actions/workflows/build.yml/badge.svg)](https://github.com/DrMuss/best-stories-api/actions/workflows/build.yml)

An ASP.NET Core service returning the best _n_ Hacker News stories by score.

Design decisions, and the dependencies deliberately not taken, are recorded in [DESIGN.md](DESIGN.md).

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
