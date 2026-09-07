namespace BestStories.Api.HackerNews;

public sealed class HackerNewsOptions
{
    public const string SectionName = "HackerNews";

    // The trailing slash is load-bearing: HttpClient.BaseAddress drops the last segment
    // without it, taking the v0 with it.
    public string BaseUrl { get; init; } = string.Empty;

    // The bound on how stale a score may get, and the upstream call rate, are the same number.
    public TimeSpan RefreshInterval { get; init; } = TimeSpan.FromMinutes(1);

    public TimeSpan UpstreamAttemptTimeout { get; init; } = TimeSpan.FromSeconds(10);

    // Jitter is applied on top, so a fleet does not retry in step and become the second outage.
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(2);

    // A pure timer means a service nobody is using still calls Hacker News forever. After this
    // long without a request, cycles are skipped until someone asks again.
    public TimeSpan IdleTimeout { get; init; } = TimeSpan.FromMinutes(10);

    // Politeness towards a free, unauthenticated API rather than a throughput figure.
    public int MaxConcurrentItemFetches { get; init; } = 10;
}
