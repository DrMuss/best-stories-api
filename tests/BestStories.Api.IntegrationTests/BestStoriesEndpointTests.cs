using System.Net;
using System.Net.Http.Json;
using BestStories.Api.Contracts;
using BestStories.Api.IntegrationTests.TestSupport;
using Shouldly;

namespace BestStories.Api.IntegrationTests;

public class BestStoriesEndpointTests
{
    [Fact]
    public async Task GetStories_ReturnsEmptyArray_WhenNoStoriesAvailable()
    {
        using var api = new ApiWithStubbedHackerNews();
        api.Upstream.RespondsWithBestStoryIds();

        var response = await api.CreateClient().GetAsync("/stories");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("[]");
    }

    [Fact]
    public async Task GetStories_ReturnsStoriesFromUpstream()
    {
        using var api = new ApiWithStubbedHackerNews();
        api.Upstream
            .RespondsWithBestStoryIds(21233041, 21233042, 21233043)
            .RespondsWithItem(21233041, """
                {
                  "by": "ismaildonmez",
                  "descendants": 572,
                  "id": 21233041,
                  "score": 1716,
                  "time": 1570887781,
                  "title": "A uBlock Origin update was rejected from the Chrome Web Store",
                  "type": "story",
                  "url": "https://github.com/uBlockOrigin/uBlock-issues/issues/745"
                }
                """)
            .RespondsWithItem(21233042, """
                {
                  "by": "pg",
                  "descendants": 12,
                  "id": 21233042,
                  "score": 84,
                  "time": 1570887782,
                  "title": "The second story",
                  "type": "story",
                  "url": "https://example.com/second"
                }
                """)
            .RespondsWithItem(21233043, """
                {
                  "by": "dhh",
                  "descendants": 3,
                  "id": 21233043,
                  "score": 41,
                  "time": 1570887783,
                  "title": "The third story",
                  "type": "story",
                  "url": "https://example.com/third"
                }
                """);

        var stories = await api.CreateClient().GetFromJsonAsync<StoryDto[]>("/stories?n=3");

        stories.ShouldNotBeNull();
        stories.Length.ShouldBe(3);

        var first = stories[0];
        first.Title.ShouldBe("A uBlock Origin update was rejected from the Chrome Web Store");
        first.Uri.ShouldBe("https://github.com/uBlockOrigin/uBlock-issues/issues/745");
        first.PostedBy.ShouldBe("ismaildonmez");
        first.Time.ShouldBe(new DateTimeOffset(2019, 10, 12, 13, 43, 1, TimeSpan.Zero));
        first.Score.ShouldBe(1716);
        first.CommentCount.ShouldBe(572);

        stories[1].Title.ShouldBe("The second story");
        stories[2].Title.ShouldBe("The third story");
    }

    [Fact]
    public async Task GetStories_FetchesOnlyTheRequestedNumberOfItems()
    {
        using var api = new ApiWithStubbedHackerNews();
        api.Upstream
            .RespondsWithBestStoryIds(1, 2, 3)
            .RespondsWithItem(1, """{"by":"a","descendants":0,"id":1,"score":9,"time":1570887781,"title":"One","type":"story","url":"https://example.com/1"}""");

        await api.CreateClient().GetFromJsonAsync<StoryDto[]>("/stories?n=1");

        api.Upstream.RequestedPaths.ShouldBe(["beststories.json", "item/1.json"]);
    }
}
