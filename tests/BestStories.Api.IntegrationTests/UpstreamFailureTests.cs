using System.Net;
using System.Net.Http.Json;
using BestStories.Api.Contracts;
using BestStories.Api.IntegrationTests.TestSupport;
using BestStories.Api.Stories;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BestStories.Api.IntegrationTests;

public class UpstreamFailureTests
{
    [Fact]
    public async Task Endpoint_ServesLastGoodSnapshot_WhenRefreshFails()
    {
        using var api = AWarmApi();
        var client = api.CreateClient();

        api.Upstream.FailsEveryItemRequestWith(HttpStatusCode.InternalServerError);
        await ARefreshThatFails(api);

        var stories = await client.GetFromJsonAsync<StoryDto[]>("/stories?n=3");

        stories.ShouldNotBeNull();
        stories.Select(story => story.Title).ShouldBe(["First", "Second", "Third"]);
    }

    [Fact]
    public async Task Refresh_DiscardsCycle_WhenAnyItemFetchFails()
    {
        using var api = AWarmApi();
        api.CreateClient();

        // One of the three keeps failing; the other two answer perfectly well.
        api.Upstream.FailsItemWith(2, HttpStatusCode.InternalServerError);
        await ARefreshThatFails(api);

        // Not a snapshot of the two that did arrive: partial transport failure is total failure.
        api.Services.GetRequiredService<IStorySnapshot>().Current.Count.ShouldBe(3);
    }

    [Fact]
    public async Task Refresh_SkipsNullItems_AndStillPublishes()
    {
        using var api = AWarmApi();
        api.CreateClient();

        // Hacker News answers an id it no longer has with 200 and a body of null.
        api.Upstream.RespondsWithItem(2, "null");
        await RefreshAsync(api);

        api.Services.GetRequiredService<IStorySnapshot>().Current.Count.ShouldBe(2);
    }

    [Theory]
    [InlineData("deleted")]
    [InlineData("dead")]
    public async Task Refresh_SkipsDeletedAndDeadItems(string withdrawal)
    {
        using var api = AWarmApi();
        api.CreateClient();

        api.Upstream.RespondsWithItem(2, $$"""
            {"by":"a","descendants":0,"id":2,"score":99,"time":1570887781,"title":"Withdrawn","type":"story","{{withdrawal}}":true}
            """);
        await RefreshAsync(api);

        var snapshot = api.Services.GetRequiredService<IStorySnapshot>().Current;

        snapshot.Count.ShouldBe(2);
        snapshot.ShouldNotContain(story => story.Title == "Withdrawn");
    }

    [Fact]
    public async Task Refresh_RecoversAfterUpstreamReturns()
    {
        using var api = AWarmApi();
        api.CreateClient();

        api.Upstream.FailsEveryItemRequestWith(HttpStatusCode.ServiceUnavailable);
        await ARefreshThatFails(api);

        api.Upstream.Recovers().RespondsWithStory(1, score: 5000, title: "Newly top");
        await RefreshAsync(api);

        api.Services.GetRequiredService<IStorySnapshot>().Current[0].Title.ShouldBe("Newly top");
    }

    [Fact]
    public async Task ItemFetch_RetriesTransientFailure_ThenSucceeds()
    {
        using var api = AWarmApi();
        api.CreateClient();
        var requestsBefore = api.Upstream.RequestsFor("item/1.json");

        api.Upstream.FailsTheNextItemRequestsWith(1, HttpStatusCode.BadGateway);
        await RefreshAsync(api);

        api.Upstream.RequestsFor("item/1.json").ShouldBe(requestsBefore + 2);
        api.Services.GetRequiredService<IStorySnapshot>().Current.Count.ShouldBe(3);
    }

    [Fact]
    public async Task Refresh_DiscardsCycle_WhenUpstreamReturnsMalformedJson()
    {
        using var api = AWarmApi();
        api.CreateClient();

        api.Upstream.RespondsWithItem(2, "{ this is not json");
        await ARefreshThatFails(api);

        api.Services.GetRequiredService<IStorySnapshot>().Current.Count.ShouldBe(3);
    }

    [Fact]
    public async Task Refresh_DiscardsCycle_WhenUpstreamIsTooSlowToAnswer()
    {
        using var api = AWarmApi();
        api.CreateClient();

        // The attempt timeout is one second in tests.
        api.Upstream.DelaysItemResponsesBy(TimeSpan.FromSeconds(2));
        await ARefreshThatFails(api);

        api.Services.GetRequiredService<IStorySnapshot>().Current.Count.ShouldBe(3);
    }

    private static ApiWithStubbedHackerNews AWarmApi()
    {
        var api = new ApiWithStubbedHackerNews();

        api.Upstream
            .RespondsWithBestStoryIds(1, 2, 3)
            .RespondsWithStory(1, score: 84, title: "Third")
            .RespondsWithStory(2, score: 1716, title: "First")
            .RespondsWithStory(3, score: 417, title: "Second");

        return api;
    }

    private static Task RefreshAsync(ApiWithStubbedHackerNews api) =>
        api.Services.GetRequiredService<IStoryRefresher>().RefreshAsync(CancellationToken.None);

    private static async Task ARefreshThatFails(ApiWithStubbedHackerNews api) =>
        await Should.ThrowAsync<Exception>(() => RefreshAsync(api));
}
