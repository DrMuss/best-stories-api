using System.Net;
using System.Net.Http.Json;
using BestStories.Api.Contracts;
using BestStories.Api.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BestStories.Api.IntegrationTests;

public class BestStoriesEndpointTests
{
    // The story the brief prints as its example response, as Hacker News returns it.
    private const string BriefExampleItem = """
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
        """;

    [Fact]
    public async Task ServingRequests_DoesNotCallHackerNews()
    {
        using var api = new ApiWithStubbedHackerNews();
        api.Upstream
            .RespondsWithBestStoryIds(1, 2, 3)
            .RespondsWithStory(1, score: 84)
            .RespondsWithStory(2, score: 1716)
            .RespondsWithStory(3, score: 12);

        var client = api.CreateClient();
        var callsMadeBuildingTheSnapshot = api.Upstream.UpstreamCallCount;

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 100).Select(_ => client.GetAsync("/stories?n=3")));

        responses.ShouldAllBe(response => response.StatusCode == HttpStatusCode.OK);
        // Served from the snapshot, not served empty: zero upstream calls has to mean the
        // stories came from memory, not that there were none to return.
        foreach (var response in responses)
        {
            (await response.Content.ReadFromJsonAsync<StoryDto[]>())!.Length.ShouldBe(3);
        }

        callsMadeBuildingTheSnapshot.ShouldBe(4);
        api.Upstream.UpstreamCallCount.ShouldBe(callsMadeBuildingTheSnapshot);
    }

    [Fact]
    public async Task GetStories_ReturnsEmptyArray_WhenNoStoriesAvailable()
    {
        using var api = new ApiWithStubbedHackerNews();
        api.Upstream.RespondsWithBestStoryIds();

        var response = await api.CreateClient().GetAsync("/stories?n=10");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("[]");
    }

    [Fact]
    public async Task GetStories_ReturnsStoriesFromUpstream()
    {
        using var api = new ApiWithStubbedHackerNews();
        api.Upstream
            .RespondsWithBestStoryIds(21233041, 21233042, 21233043)
            .RespondsWithItem(21233041, BriefExampleItem)
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
    public async Task BuildingTheSnapshot_FetchesEveryStoryToRankByScore()
    {
        using var api = new ApiWithStubbedHackerNews();
        api.Upstream
            .RespondsWithBestStoryIds(1, 2, 3)
            .RespondsWithStory(1, score: 9)
            .RespondsWithStory(2, score: 8)
            .RespondsWithStory(3, score: 7);

        api.CreateClient();

        api.Upstream.RequestedPaths.ShouldBe(
            ["beststories.json", "item/1.json", "item/2.json", "item/3.json"],
            ignoreOrder: true);
    }

    [Fact]
    public async Task GetStories_ReturnsStoriesInDescendingScoreOrder()
    {
        using var api = new ApiWithStubbedHackerNews();
        api.Upstream
            .RespondsWithBestStoryIds(1, 2, 3, 4)
            .RespondsWithStory(1, score: 84, title: "Third")
            .RespondsWithStory(2, score: 1716, title: "First")
            .RespondsWithStory(3, score: 12, title: "Fourth")
            .RespondsWithStory(4, score: 417, title: "Second");

        var stories = await api.CreateClient().GetFromJsonAsync<StoryDto[]>("/stories?n=4");

        stories.ShouldNotBeNull();
        stories.Select(story => story.Title).ShouldBe(["First", "Second", "Third", "Fourth"]);
        stories.Select(story => story.Score).ShouldBe([1716, 417, 84, 12]);
    }

    [Fact]
    public async Task GetStories_ReturnsTheHighestScoringStories_WhenNIsSmallerThanTheList()
    {
        using var api = new ApiWithStubbedHackerNews();
        api.Upstream
            .RespondsWithBestStoryIds(1, 2, 3)
            .RespondsWithStory(1, score: 84, title: "Middle")
            .RespondsWithStory(2, score: 1716, title: "Best")
            .RespondsWithStory(3, score: 12, title: "Worst");

        var stories = await api.CreateClient().GetFromJsonAsync<StoryDto[]>("/stories?n=2");

        stories.ShouldNotBeNull();
        stories.Select(story => story.Title).ShouldBe(["Best", "Middle"]);
    }

    [Fact]
    public async Task GetStories_ReturnsWhatIsAvailable_WhenNExceedsTheStoriesUpstreamOffers()
    {
        using var api = new ApiWithStubbedHackerNews();
        api.Upstream
            .RespondsWithBestStoryIds(1, 2, 3)
            .RespondsWithStory(1, score: 84)
            .RespondsWithStory(2, score: 1716)
            .RespondsWithStory(3, score: 12);

        var response = await api.CreateClient().GetAsync("/stories?n=1000");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<StoryDto[]>())!.Length.ShouldBe(3);
    }

    [Fact]
    public async Task GetStories_MatchesBriefExample()
    {
        using var api = new ApiWithStubbedHackerNews();
        api.Upstream
            .RespondsWithBestStoryIds(21233041)
            .RespondsWithItem(21233041, BriefExampleItem);

        var response = await api.CreateClient().GetAsync("/stories?n=1");

        (await response.Content.ReadAsStringAsync()).ShouldBe(
            """
            [{"title":"A uBlock Origin update was rejected from the Chrome Web Store","uri":"https://github.com/uBlockOrigin/uBlock-issues/issues/745","postedBy":"ismaildonmez","time":"2019-10-12T13:43:01+00:00","score":1716,"commentCount":572}]
            """);
    }

    [Fact]
    public async Task GetStories_OmitsUri_WhenAskHnPostHasNoUrl()
    {
        using var api = new ApiWithStubbedHackerNews();
        api.Upstream
            .RespondsWithBestStoryIds(21233041)
            .RespondsWithItem(21233041, """
                {"by":"pg","descendants":4,"id":21233041,"score":100,"time":1570887781,"title":"Ask HN: anything?","type":"story"}
                """);

        var response = await api.CreateClient().GetAsync("/stories?n=1");

        (await response.Content.ReadAsStringAsync()).ShouldNotContain("uri");
    }

    [Theory]
    [InlineData("/stories")]
    [InlineData("/stories?n=0")]
    [InlineData("/stories?n=-1")]
    [InlineData("/stories?n=abc")]
    public async Task GetStories_ReturnsProblemDetails_WhenNIsInvalid(string requestUri)
    {
        using var api = new ApiWithStubbedHackerNews();
        api.Upstream.RespondsWithBestStoryIds();

        var response = await api.CreateClient().GetAsync(requestUri);

        await ShouldBeTheInvalidStoryCountProblem(response);
    }

    [Fact]
    public async Task GetStories_ReturnsProblemDetails_WhenNOverflowsInt()
    {
        using var api = new ApiWithStubbedHackerNews();
        api.Upstream.RespondsWithBestStoryIds();

        var response = await api.CreateClient().GetAsync("/stories?n=99999999999999999999");

        await ShouldBeTheInvalidStoryCountProblem(response);
    }

    // Every way of getting n wrong answers identically, so the assertion is shared.
    private static async Task ShouldBeTheInvalidStoryCountProblem(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        problem.ShouldNotBeNull();
        problem.Status.ShouldBe(400);
        problem.Title.ShouldBe("Invalid story count");
        problem.Detail.ShouldBe("n is required and must be a positive integer.");
    }
}
