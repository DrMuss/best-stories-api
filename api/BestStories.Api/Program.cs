using BestStories.Api.Contracts;
using BestStories.Api.Endpoints;
using BestStories.Api.HackerNews;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(ApiDocumentation.Describe);
builder.Services.AddHealthChecks();

// The v0 in the base URL is a version pin on someone else's contract, so it lives in
// configuration where it is visible and overridable. The trailing slash is load-bearing:
// HttpClient.BaseAddress drops the last segment without it.
var hackerNewsBaseUrl = builder.Configuration["HackerNews:BaseUrl"]
    ?? throw new InvalidOperationException("HackerNews:BaseUrl is not configured.");

builder.Services.AddHttpClient<HackerNewsClient>(
    httpClient => httpClient.BaseAddress = new Uri(hackerNewsBaseUrl));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // The document; Scalar renders it. Root redirects so a reviewer landing on
    // the host sees the API reference rather than a 404.
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapGet("/", () => Results.Redirect("/scalar")).ExcludeFromDescription();
}

app.UseHttpsRedirection();

app.MapHealthChecks("/health");
app.MapEndpoints();

app.Run();
public partial class Program { }
