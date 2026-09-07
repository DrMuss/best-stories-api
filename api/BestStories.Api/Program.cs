using BestStories.Api.Contracts;
using BestStories.Api.Endpoints;
using BestStories.Api.HackerNews;
using BestStories.Api.OpenApi;
using BestStories.Api.Stories;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(ApiDocumentation.Describe);
builder.Services.AddHealthChecks();

// Validated as the app starts rather than when the first refresh runs, so a bad setting is a
// startup failure with a message instead of a background exception nobody is watching for.
builder.Services.AddOptions<HackerNewsOptions>()
    .BindConfiguration(HackerNewsOptions.SectionName)
    .ValidateDataAnnotations()
    .Validate(
        options => options.RefreshInterval >= TimeSpan.FromSeconds(1)
                   && options.RefreshInterval < TimeSpan.FromDays(1),
        $"{HackerNewsOptions.SectionName}:RefreshInterval must be at least one second and less "
        + "than a day. Note that a bare number is read as days: \"30\" means 30 days, not 30 seconds.")
    .ValidateOnStart();

builder.Services.AddHttpClient<HackerNewsClient>((services, httpClient) =>
        httpClient.BaseAddress = new Uri(services.GetRequiredService<IOptions<HackerNewsOptions>>().Value.BaseUrl))
    // Retry with backoff and jitter, a circuit breaker, and timeouts, ordered correctly. Retry
    // is the one resilience pattern that can cause the outage it prevents: three retries across
    // a whole refresh, against an upstream already struggling, is us adding to the load. The
    // breaker and the fan-out cap are what make it safe.
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

app.MapHealthChecks("/health");
app.MapEndpoints();

app.Run();
public partial class Program { }
