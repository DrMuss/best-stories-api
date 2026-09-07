# CLAUDE.md
Constraints for this repository, set before implementation. Rationale for each is recorded in `DESIGN.md`.

## Project

`best-stories-api` — ASP.NET Core service returning the best `n` Hacker News stories by score.
Design decisions and deliberate omissions are recorded in `DESIGN.md`. Read it before proposing changes.

## Governing constraint

The graded requirement is servicing large request volumes without overloading the Hacker News API.
Complexity spent there is proportionate. Complexity spent anywhere else is not.

Before adding anything, apply:

- Serves "efficiently, without overloading" → build it
- Production concern the brief does not ask for → one line in `DESIGN.md`, not code
- Needs a container, broker, database or second process to run → `DESIGN.md`

## Dependencies

No third-party package without a row in the `DESIGN.md` decision table stating what it buys.

Already considered and rejected: MediatR, FastEndpoints, FluentValidation, Ardalis.Result, Redis, WireMock.
Do not reintroduce them.

## Architecture invariants

Do not break these without changing `DESIGN.md` first.

- The request path never calls Hacker News. Requests read an immutable snapshot.
- The snapshot is replaced by reference swap, never mutated in place.
- Refresh runs in a `PeriodicTimer` loop. Cycles must not overlap.
- The 500-item fan-out is concurrency-bounded.
- `beststories.json` ordering is undocumented. Always sort by score explicitly.
- `time` serialises as ISO-8601 with offset (`2019-10-12T13:43:01+00:00`), not `Z`.
- `url` is absent on Ask HN posts. `descendants` may be absent or null.

## Testing

- Write the test name before the implementation. The test is the definition of done.
- Unit tests for pure functions. Integration tests through `WebApplicationFactory`.
- Upstream is stubbed with a custom `HttpMessageHandler`. Nothing hits the network except the single
  test tagged `[Trait("Category", "Network")]`, excluded in CI by `--filter Category!=Network`.
- `ServingRequests_DoesNotCallHackerNews` must stay green. It is the brief's requirement as an assertion.

## Commits

- One capability per commit. Every commit green.
- Subject states the capability. Body states the decision and why.
- Do not squash. The history is part of the deliverable.

## Code

- Nullable enabled, warnings as errors, set in `Directory.Build.props`.
- Explicit over clever.
- Comments explain why, never restate what.
