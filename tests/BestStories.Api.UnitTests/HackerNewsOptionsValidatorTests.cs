using BestStories.Api.HackerNews;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BestStories.Api.UnitTests;

public class HackerNewsOptionsValidatorTests
{
    [Fact]
    public void Options_AreValid_WithTheShippedDefaults()
    {
        Validate(Configured()).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Options_WithNothingConfigured_FailOnTheBaseUrl()
    {
        // Every other setting has a usable default; the base URL deliberately does not, so a
        // section that was never configured is a startup failure rather than a silent default.
        ShouldFailMentioning(new HackerNewsOptions(), "BaseUrl");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("hacker-news.firebaseio.com/v0/")]
    public void BaseUrl_MustBeAnAbsoluteUrl(string baseUrl)
    {
        ShouldFailMentioning(Configured(baseUrl: baseUrl), "BaseUrl");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-60)]
    [InlineData(0.5)]
    public void RefreshInterval_MustBeAtLeastOneSecond(double seconds)
    {
        ShouldFailMentioning(
            Configured(refreshInterval: TimeSpan.FromSeconds(seconds)), "RefreshInterval");
    }

    [Fact]
    public void RefreshInterval_OfExactlyOneSecond_IsAllowed()
    {
        Validate(Configured(refreshInterval: TimeSpan.FromSeconds(1))).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void RefreshInterval_OfExactlyOneDay_IsRejected()
    {
        ShouldFailMentioning(Configured(refreshInterval: TimeSpan.FromDays(1)), "RefreshInterval");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void MaxConcurrentItemFetches_AllowsBothEndsOfItsRange(int cap)
    {
        Validate(Configured(maxConcurrentItemFetches: cap)).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void IdleTimeout_OfZero_IsRejected()
    {
        ShouldFailMentioning(Configured(idleTimeout: TimeSpan.Zero), "IdleTimeout");
    }

    [Fact]
    public void RetryDelay_OfZero_IsAllowed()
    {
        // No wait before a retry is aggressive but coherent; only a negative delay is nonsense.
        Validate(Configured(retryDelay: TimeSpan.Zero)).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void RefreshInterval_OfADayOrMore_IsRejectedAsTheBareNumberMistake()
    {
        // "30" in configuration binds to thirty days, which is the mistake an operator makes
        // when they mean thirty seconds.
        var failure = ShouldFailMentioning(
            Configured(refreshInterval: TimeSpan.FromDays(30)), "RefreshInterval");

        failure.ShouldContain("30 days, not 30 seconds");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public void MaxConcurrentItemFetches_MustBeAWorkableCap(int cap)
    {
        ShouldFailMentioning(Configured(maxConcurrentItemFetches: cap), "MaxConcurrentItemFetches");
    }

    [Fact]
    public void UpstreamAttemptTimeout_MustBeGreaterThanZero()
    {
        ShouldFailMentioning(Configured(upstreamAttemptTimeout: TimeSpan.Zero), "UpstreamAttemptTimeout");
    }

    [Fact]
    public void RetryDelay_CannotBeNegative()
    {
        ShouldFailMentioning(Configured(retryDelay: TimeSpan.FromSeconds(-1)), "RetryDelay");
    }

    [Fact]
    public void EverySettingThatIsWrong_IsReported_NotJustTheFirst()
    {
        var result = Validate(Configured(baseUrl: "", maxConcurrentItemFetches: 0));

        result.Failures.ShouldNotBeNull().Count().ShouldBe(2);
    }

    private static ValidateOptionsResult Validate(HackerNewsOptions options) =>
        new HackerNewsOptionsValidator().Validate(name: null, options);

    private static string ShouldFailMentioning(HackerNewsOptions options, string setting)
    {
        var result = Validate(options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain($"HackerNews:{setting}");

        return result.FailureMessage!;
    }

    private static HackerNewsOptions Configured(
        string baseUrl = "https://hacker-news.firebaseio.com/v0/",
        TimeSpan? refreshInterval = null,
        int maxConcurrentItemFetches = 10,
        TimeSpan? upstreamAttemptTimeout = null,
        TimeSpan? retryDelay = null,
        TimeSpan? idleTimeout = null) =>
        new()
        {
            BaseUrl = baseUrl,
            RefreshInterval = refreshInterval ?? TimeSpan.FromMinutes(1),
            MaxConcurrentItemFetches = maxConcurrentItemFetches,
            UpstreamAttemptTimeout = upstreamAttemptTimeout ?? TimeSpan.FromSeconds(10),
            RetryDelay = retryDelay ?? TimeSpan.FromSeconds(2),
            IdleTimeout = idleTimeout ?? TimeSpan.FromMinutes(10)
        };
}
