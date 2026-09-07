using BestStories.Api.IntegrationTests.TestSupport;
using BestStories.Api.Stories;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BestStories.Api.IntegrationTests;

public class StoryRefresherTests
{
    [Theory]
    [InlineData(3)]
    [InlineData(10)]
    public async Task Refresh_LimitsConcurrentUpstreamCalls(int cap)
    {
        using var api = AnApiOfferingFiftyStories(maxConcurrentItemFetches: cap);
        await api.CreateReadyClientAsync();

        // The startup refresh has finished; hold the next one open to see its shape.
        api.Upstream.HoldItemResponses();
        var refresh = api.Services.GetRequiredService<IStoryRefresher>().RefreshAsync(CancellationToken.None);

        await api.Upstream.WaitUntilRequestsInFlightReaches(cap);
        await Task.Delay(200);

        api.Upstream.PeakConcurrentRequests.ShouldBe(cap);

        api.Upstream.ReleaseHeldResponses();
        await refresh;
    }

    [Fact]
    public async Task Refresh_StillFetchesEveryStory_WhileBoundedToAFewAtATime()
    {
        using var api = AnApiOfferingFiftyStories(maxConcurrentItemFetches: 3);
        await api.CreateReadyClientAsync();

        await api.Services.GetRequiredService<IStoryRefresher>().RefreshAsync(CancellationToken.None);

        api.Services.GetRequiredService<IStorySnapshot>().Read().Stories.Count.ShouldBe(50);
    }

    private static ApiWithStubbedHackerNews AnApiOfferingFiftyStories(int maxConcurrentItemFetches)
    {
        var api = new ApiWithStubbedHackerNews(new Dictionary<string, string?>
        {
            ["HackerNews:MaxConcurrentItemFetches"] = maxConcurrentItemFetches.ToString()
        });

        var storyIds = Enumerable.Range(1, 50).ToArray();
        api.Upstream.RespondsWithBestStoryIds(storyIds);

        foreach (var storyId in storyIds)
        {
            api.Upstream.RespondsWithStory(storyId, score: storyId);
        }

        return api;
    }
}
