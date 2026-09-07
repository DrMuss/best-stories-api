using BestStories.Api.Contracts;
using BestStories.Api.Endpoints;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using BestStories.Api.HackerNews;
using BestStories.Api.OpenApi;
using BestStories.Api.Stories;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(ApiDocumentation.Describe);
builder.Services.AddHealthChecks()
    .AddCheck<StorySnapshotReadiness>("story-snapshot", tags: ["ready"]);

// Validated as the app starts rather than when the first refresh runs, so a bad setting is a
// startup failure with a message instead of a background exception nobody is watching for.
builder.Services.AddSingleton<IValidateOptions<HackerNewsOptions>, HackerNewsOptionsValidator>();
builder.Services.AddOptions<HackerNewsOptions>()
    .BindConfiguration(HackerNewsOptions.SectionName)
    .ValidateOnStart();

builder.Services.AddHttpClient<HackerNewsClient>((services, httpClient) =>
        httpClient.BaseAddress = new Uri(services.GetRequiredService<IOptions<HackerNewsOptions>>().Value.BaseUrl))
    // Retry is the one resilience pattern that can cause the outage it prevents: three retries
    // across a whole refresh, against an upstream already struggling, is us adding to the load.
    // The breaker and the fan-out cap are what make it safe.
    .AddStandardResilienceHandler()
    .Configure((resilience, services) =>
    {
        var hackerNews = services.GetRequiredService<IOptions<HackerNewsOptions>>().Value;

        resilience.AttemptTimeout.Timeout = hackerNews.UpstreamAttemptTimeout;
        resilience.Retry.Delay = hackerNews.RetryDelay;
        resilience.TotalRequestTimeout.Timeout = hackerNews.UpstreamAttemptTimeout * 4;
        resilience.CircuitBreaker.SamplingDuration = hackerNews.UpstreamAttemptTimeout * 2;
    });

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IStorySnapshot, StorySnapshot>();
builder.Services.AddSingleton<IStoryRequests, StoryRequests>();
builder.Services.AddSingleton<IStoryRefresher, StoryRefresher>();
builder.Services.AddHostedService<StoryRefreshService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Root redirects so a reviewer landing on the host meets the API reference, not a 404.
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapGet("/", () => Results.Redirect("/scalar")).ExcludeFromDescription();
}

app.UseHttpsRedirection();

app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.MapEndpoints();

app.Run();
public partial class Program { }
