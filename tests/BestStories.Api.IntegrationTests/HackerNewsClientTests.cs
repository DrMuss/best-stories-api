using BestStories.Api.HackerNews;
using BestStories.Api.IntegrationTests.TestSupport;
using Shouldly;

namespace BestStories.Api.IntegrationTests;

public class HackerNewsClientTests
{
    [Fact]
    public async Task HackerNewsClient_FetchesBestStoryIds()
    {
        var upstream = new HackerNewsStub().RespondsWithBestStoryIds(21233041, 21233042);
        using var httpClient = new HttpClient(upstream) { BaseAddress = new Uri("https://hacker-news.test/v0/") };
        var client = new HackerNewsClient(httpClient);

        var storyIds = await client.GetBestStoryIdsAsync(CancellationToken.None);

        storyIds.ShouldBe([21233041, 21233042]);
        upstream.RequestedPaths.ShouldBe(["beststories.json"]);
    }

    [Fact]
    public async Task HackerNewsClient_TreatsANullStoryList_AsNoStories()
    {
        var upstream = new HackerNewsStub().RespondsWith("beststories.json", "null");
        using var httpClient = new HttpClient(upstream) { BaseAddress = new Uri("https://hacker-news.test/v0/") };
        var client = new HackerNewsClient(httpClient);

        var storyIds = await client.GetBestStoryIdsAsync(CancellationToken.None);

        storyIds.ShouldBeEmpty();
    }

    // The stub proves we parse what we think Hacker News sends. This proves what it actually
    // sends still matches. Categorised so a build can exclude it and stay green through a bad
    // day upstream.
    [Fact]
    [Trait("Category", "Network")]
    public async Task HackerNewsClient_MatchesLiveContract()
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri("https://hacker-news.firebaseio.com/v0/") };
        var client = new HackerNewsClient(httpClient);

        var storyIds = await client.GetBestStoryIdsAsync(CancellationToken.None);
        storyIds.ShouldNotBeEmpty();

        var item = await client.GetItemAsync(storyIds[0], CancellationToken.None);

        item.ShouldNotBeNull();
        item.Id.ShouldBe(storyIds[0]);
        item.Type.ShouldBe("story");
        item.Title.ShouldNotBeNullOrWhiteSpace();
        item.By.ShouldNotBeNullOrWhiteSpace();
        item.Time.ShouldBeGreaterThan(0);
    }
}
