using System.ComponentModel.DataAnnotations;

namespace BestStories.Api.Stories;

public sealed class HackerNewsOptions
{
    public const string SectionName = "HackerNews";

    // The trailing slash is load-bearing: HttpClient.BaseAddress drops the last segment
    // without it, taking the v0 with it.
    [Required]
    public string BaseUrl { get; init; } = string.Empty;

    // The bound on how stale a score may get, and the upstream call rate, are the same number.
    public TimeSpan RefreshInterval { get; init; } = TimeSpan.FromMinutes(1);

    // Politeness towards a free, unauthenticated API rather than a throughput figure.
    [Range(1, 100)]
    public int MaxConcurrentItemFetches { get; init; } = 10;
}
