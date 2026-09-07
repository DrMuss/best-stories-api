# Design

## Deliberate omissions

| Considered | Decision | Reason |
|---|---|---|
| MediatR | Rejected | One query, one handler, one caller. Indirection buys nothing. Also now commercially licensed (v13+, Lucky Penny Software) — a licence question is an unforced complication in a hiring artefact |
| FastEndpoints | Rejected | Brief says ASP.NET Core. Third-party framework means the reviewer learns a library to review 40 lines. The REPR benefit is achievable with a Minimal API endpoint class |
| FluentValidation + validation pipeline behaviour | Rejected | One integer parameter |
| MediatR logging behaviour | Rejected | ASP.NET Core already logs requests. Telemetry that matters here is on the refresh path |
| Ardalis.Result | Rejected | Principle is right; this endpoint has one negative outcome. A dependency carrying one case |
| Redis / distributed cache | Rejected | Single instance. Belongs in the scale-out discussion |
| Inbound rate limiting | Rejected | Once the cache is warm, inbound load never reaches Hacker News. The cache *is* the decoupling. Inbound limiting would protect our own CPU, which is a different requirement than the one set |
| Firebase change subscription (streaming) | Rejected for now | Official .NET Firebase SDK does not cover Realtime Database streaming — would need a community package. Long-lived stream needs reconnect **and resync on reconnect**, converting a freshness problem into a correctness problem. May not survive a corporate proxy on the reviewer's machine |
| Hand-built HTML UI | Rejected | Backend test. Scope outside the brief in a skill not being assessed |
| Clean Architecture 4-project split (Domain/Application/Infrastructure/Api) | Rejected | One endpoint. Folders within one project carry the same separation at a tenth the navigation cost |
| Moq | Rejected | v4.20 shipped SponsorLink, which read the developer's git email at build time. Withdrawn since, but the trust question outlives the code, and it is the same unforced licence complication as MediatR. NSubstitute costs nothing to switch to |
| FluentAssertions | Rejected | v8 moved to a paid commercial licence (Xceed). Shouldly is BSD-licensed and reads the same |
| WireMock / mock HTTP server | Rejected | A ~20-line `HttpMessageHandler` stub programs per-URL responses and counts calls. A dependency to avoid twenty lines |
| API versioning (`Asp.Versioning.Http`, `/v1/` route prefix, version pinned to Hacker News) | Rejected | A version number is a promise to *our* consumers about *our* contract; Hacker News' `v0` is a property of theirs. The two lifecycles are independent — an upstream change we absorb in the mapper is invisible to callers, and a rename of our own would need a bump while upstream sat still. Tying them means bumping for changes nobody can observe, or being unable to bump for changes they can. The versioning package then buys negotiation, version readers and a deprecation policy for one endpoint, all inert. Hacker News is the cautionary tale: `v0` since 2014, never bumped. What actually insulates callers is that the response DTO is ours rather than a passthrough of upstream item JSON, so an upstream break is one mapping function to fix |


## Decisions taken at setup

Tooling and configuration chosen at setup, and therefore present in the first commit.

| Decision | Rationale |
|---|---|
| Scalar (`Scalar.AspNetCore`) for the OpenAPI UI | .NET 9 removed the bundled Swagger UI: the framework generates the document, rendering is the caller's problem. Scalar renders the built-in document with no second generator, so Swashbuckle is not needed. `/` redirects to it, in Development only, so a reviewer opening the host can exercise the API rather than meet a 404 |
| xUnit | Default for .NET, no learning cost for the reviewer. `WebApplicationFactory<Program>` needs `public partial class Program` — the one line added to `Program.cs` |
| NSubstitute | No SponsorLink history, no licence question, terser syntax than Moq. Little used regardless: upstream is stubbed with a hand-rolled `HttpMessageHandler` |
| Shouldly | Assertion failures name the expression that failed. BSD-licensed, which FluentAssertions v8 no longer is |
| coverlet (`XPlat Code Coverage`) + ReportGenerator | coverlet writes per-method cyclomatic complexity into the Cobertura report; ReportGenerator's Risk Hotspots turns that plus per-method coverage into **CRAP scores**. Two CLI tools, no server, no account |
| Stryker.NET | Coverage proves a line ran; mutation proves the test would fail if the line were wrong. Run locally, score quoted in the README — scheduled nightly rather than per-commit in a real deployment |
| Central package management (`Directory.Packages.props`) | Three projects already share seven packages. Versions drift silently otherwise; one file makes a mismatch a merge conflict instead of a runtime surprise |
| Hacker News base URL in `appsettings.json` (`HackerNews:BaseUrl`) | The `v0` in `https://hacker-news.firebaseio.com/v0/` is a version pin. Held in configuration it is one visible, deliberate line a reviewer can find and a deployment can override; hard-coded it becomes an accident of a string literal repeated wherever a call is made. Trailing slash is load-bearing — `HttpClient.BaseAddress` drops the last segment without it |
