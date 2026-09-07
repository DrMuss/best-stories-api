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
`GET /health` reports liveness.

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

## Tests

```bash
dotnet test                                # whole solution
dotnet test --filter "Category!=Network"   # as CI runs it, excluding live Hacker News
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
