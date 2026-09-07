using BestStories.Api.HackerNews;
using BestStories.Api.Stories;
using Shouldly;

using static BestStories.Api.UnitTests.TestSupport.HackerNewsItems;

namespace BestStories.Api.UnitTests;

public class StoryRankerTests
{
    [Fact]
    public void Stories_AreOrderedByScoreDescending()
    {
        HackerNewsItem?[] items =
        [
            AnItemWith(id: 1, score: 84),
            AnItemWith(id: 2, score: 1716),
            AnItemWith(id: 3, score: 417)
        ];

        var ranked = StoryRanker.RankBestFirst(items);

        ranked.Select(story => story.Score).ShouldBe([1716, 417, 84]);
    }

    [Fact]
    public void Stories_WithEqualScores_AreOrderedById()
    {
        HackerNewsItem?[] items =
        [
            AnItemWith(id: 30, score: 100, title: "Third by id"),
            AnItemWith(id: 10, score: 100, title: "First by id"),
            AnItemWith(id: 20, score: 100, title: "Second by id")
        ];

        var ranked = StoryRanker.RankBestFirst(items);

        ranked.Select(story => story.Title).ShouldBe(["First by id", "Second by id", "Third by id"]);
    }

    [Fact]
    public void Stories_IgnoresItemsThatAreNotStories()
    {
        HackerNewsItem?[] items =
        [
            AnItemWith(id: 1, type: "job", title: "A job posting"),
            AnItemWith(id: 2, type: "story", title: "A story"),
            AnItemWith(id: 3, type: "poll", title: "A poll")
        ];

        var ranked = StoryRanker.RankBestFirst(items);

        ranked.Select(story => story.Title).ShouldBe(["A story"]);
    }

    [Fact]
    public void Stories_IgnoresItemsUpstreamNoLongerHas()
    {
        HackerNewsItem?[] items = [AnItemWith(id: 1, title: "Still there"), null];

        var ranked = StoryRanker.RankBestFirst(items);

        ranked.Select(story => story.Title).ShouldBe(["Still there"]);
    }

    [Fact]
    public void Stories_AreEmpty_WhenUpstreamOffersNothing()
    {
        StoryRanker.RankBestFirst([]).ShouldBeEmpty();
    }
}
