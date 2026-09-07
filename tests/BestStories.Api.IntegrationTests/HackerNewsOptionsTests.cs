using BestStories.Api.IntegrationTests.TestSupport;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BestStories.Api.IntegrationTests;

public class HackerNewsOptionsTests
{
    [Theory]
    [InlineData("00:00:00")]
    [InlineData("-00:01:00")]
    [InlineData("00:00:00.5")]
    // A bare number is a TimeSpan of days, so this is the one an operator types meaning seconds.
    [InlineData("30")]
    public void Startup_Fails_WhenRefreshIntervalIsOutOfBounds(string refreshInterval)
    {
        using var api = new ApiWithStubbedHackerNews(
            new Dictionary<string, string?> { ["HackerNews:RefreshInterval"] = refreshInterval });

        ShouldRefuseToStart(api, complainingAbout: "RefreshInterval");
    }

    [Fact]
    public void Startup_Fails_WhenTheHackerNewsBaseUrlIsMissing()
    {
        using var api = new ApiWithStubbedHackerNews(
            new Dictionary<string, string?> { ["HackerNews:BaseUrl"] = "" });

        ShouldRefuseToStart(api, complainingAbout: "BaseUrl");
    }

    // Startup surfaces the same validation failure more than once — the options are validated on
    // start and resolved again while the resilience pipeline is built — so the host aggregates.
    // What matters is that it refuses to start and says which setting is wrong.
    private static void ShouldRefuseToStart(ApiWithStubbedHackerNews api, string complainingAbout)
    {
        var failure = Should.Throw<Exception>(() => api.CreateClient());

        var validation = failure as OptionsValidationException
            ?? (failure as AggregateException)?.Flatten().InnerExceptions
                .OfType<OptionsValidationException>()
                .FirstOrDefault();

        validation.ShouldNotBeNull();
        validation.Message.ShouldContain(complainingAbout);
    }
}
