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

        var failure = Should.Throw<OptionsValidationException>(() => api.CreateClient());

        failure.Message.ShouldContain("RefreshInterval");
    }

    [Fact]
    public void Startup_Fails_WhenTheHackerNewsBaseUrlIsMissing()
    {
        using var api = new ApiWithStubbedHackerNews(
            new Dictionary<string, string?> { ["HackerNews:BaseUrl"] = "" });

        var failure = Should.Throw<OptionsValidationException>(() => api.CreateClient());

        failure.Message.ShouldContain("BaseUrl");
    }
}
