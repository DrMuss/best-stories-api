using BestStories.Api.HackerNews;
using Shouldly;

using static BestStories.Api.UnitTests.TestSupport.HackerNewsItems;

namespace BestStories.Api.UnitTests;

public class HackerNewsItemTests
{
    [Fact]
    public void Story_MapsUnixTimeToIso8601WithOffset()
    {
        var item = AnItemWith(time: 1570887781);

        var story = item.ToStory();

        story.Time.ShouldBe(new DateTimeOffset(2019, 10, 12, 13, 43, 1, TimeSpan.Zero));
        story.Time.Offset.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void Story_OmitsUri_WhenAskHnPostHasNoUrl()
    {
        var item = AnItemWith(url: null);

        item.ToStory().Uri.ShouldBeNull();
    }

    [Fact]
    public void Story_TreatsMissingDescendantsAsZero()
    {
        var item = AnItemWith(descendants: null);

        item.ToStory().CommentCount.ShouldBe(0);
    }

    [Fact]
    public void Story_CarriesTitleScoreAndAuthorThrough()
    {
        var item = AnItemWith(
            title: "A uBlock Origin update was rejected from the Chrome Web Store",
            by: "ismaildonmez",
            score: 1716,
            descendants: 572,
            url: "https://github.com/uBlockOrigin/uBlock-issues/issues/745");

        var story = item.ToStory();

        story.Title.ShouldBe("A uBlock Origin update was rejected from the Chrome Web Store");
        story.PostedBy.ShouldBe("ismaildonmez");
        story.Score.ShouldBe(1716);
        story.CommentCount.ShouldBe(572);
        story.Uri.ShouldBe("https://github.com/uBlockOrigin/uBlock-issues/issues/745");
    }
}
