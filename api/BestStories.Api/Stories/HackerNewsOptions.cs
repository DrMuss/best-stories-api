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
    // Written as a TimeSpan so the unit is in the value rather than the property name. The trap
    // that comes with that: TimeSpan.Parse reads a bare "30" as thirty days, so the bounds below
    // reject anything a day or longer rather than let it pass as a very stale service.
    public TimeSpan RefreshInterval { get; init; } = TimeSpan.FromMinutes(1);
}
