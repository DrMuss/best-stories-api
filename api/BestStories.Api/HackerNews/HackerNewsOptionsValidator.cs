using Microsoft.Extensions.Options;

namespace BestStories.Api.HackerNews;

public sealed class HackerNewsOptionsValidator : IValidateOptions<HackerNewsOptions>
{
    private static readonly TimeSpan ShortestRefreshInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan LongestRefreshInterval = TimeSpan.FromDays(1);
    private const int FewestConcurrentItemFetches = 1;
    private const int MostConcurrentItemFetches = 100;

    public ValidateOptionsResult Validate(string? name, HackerNewsOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            failures.Add(Setting("BaseUrl") + " is required.");
        }
        else if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _))
        {
            failures.Add(Setting("BaseUrl") + " must be an absolute URL.");
        }

        if (options.RefreshInterval < ShortestRefreshInterval
            || options.RefreshInterval >= LongestRefreshInterval)
        {
            failures.Add(Setting("RefreshInterval")
                + " must be at least one second and less than a day. Note that a bare number is"
                + " read as days: \"30\" means 30 days, not 30 seconds.");
        }

        if (options.MaxConcurrentItemFetches < FewestConcurrentItemFetches
            || options.MaxConcurrentItemFetches > MostConcurrentItemFetches)
        {
            failures.Add(Setting("MaxConcurrentItemFetches")
                + $" must be between {FewestConcurrentItemFetches} and {MostConcurrentItemFetches}.");
        }

        if (options.IdleTimeout <= TimeSpan.Zero)
        {
            failures.Add(Setting("IdleTimeout") + " must be greater than zero.");
        }

        if (options.UpstreamAttemptTimeout <= TimeSpan.Zero)
        {
            failures.Add(Setting("UpstreamAttemptTimeout") + " must be greater than zero.");
        }

        if (options.RetryDelay < TimeSpan.Zero)
        {
            failures.Add(Setting("RetryDelay") + " cannot be negative.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static string Setting(string name) => $"{HackerNewsOptions.SectionName}:{name}";
}
