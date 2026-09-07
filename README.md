# best-stories-api

[![build](https://github.com/DrMuss/best-stories-api/actions/workflows/build.yml/badge.svg)](https://github.com/DrMuss/best-stories-api/actions/workflows/build.yml)

An ASP.NET Core service returning the best _n_ Hacker News stories by score.

Design decisions, and the dependencies deliberately not taken, are recorded in [DESIGN.md](DESIGN.md).

## Running it

```bash
dotnet run --project api/BestStories.Api   # http://localhost:5128
docker compose up                          # http://localhost:8080
```

In Development the root redirects to the Scalar API reference, where endpoints can be
exercised from the browser. The OpenAPI document itself is at `/openapi/v1.json`.
`GET /health` is the only endpoint implemented so far.

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
