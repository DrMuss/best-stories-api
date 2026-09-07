using BestStories.Api.Contracts;
using BestStories.Api.Stories;
using Shouldly;

namespace BestStories.Api.UnitTests;

public class StorySnapshotTests
{
    [Fact]
    public void Snapshot_ReportsItselfNotReady_BeforeAnythingIsPublished()
    {
        var read = new StorySnapshot().Read();

        read.IsReady.ShouldBeFalse();
        read.Stories.ShouldBeEmpty();
    }

    [Fact]
    public void Snapshot_IsReady_EvenWhenUpstreamOfferedNoStories()
    {
        var snapshot = new StorySnapshot();

        snapshot.Replace([]);

        // Having asked and been given nothing is a different answer from not having asked.
        snapshot.Read().IsReady.ShouldBeTrue();
        snapshot.Read().Stories.ShouldBeEmpty();
    }

    [Fact]
    public void Snapshot_IsReplacedByReference_NotMutated()
    {
        var snapshot = new StorySnapshot();
        snapshot.Replace([AStoryTitled("First")]);

        var readerHoldingTheOldSnapshot = snapshot.Read().Stories;
        snapshot.Replace([AStoryTitled("Second")]);

        readerHoldingTheOldSnapshot.Single().Title.ShouldBe("First");
        snapshot.Read().Stories.Single().Title.ShouldBe("Second");
        snapshot.Read().Stories.ShouldNotBeSameAs(readerHoldingTheOldSnapshot);
    }

    [Fact]
    public void Snapshot_IgnoresLaterChangesToTheListItWasGiven()
    {
        var snapshot = new StorySnapshot();
        var published = new[] { AStoryTitled("First") };

        snapshot.Replace(published);
        published[0] = AStoryTitled("Swapped underneath");

        snapshot.Read().Stories.Single().Title.ShouldBe("First");
    }

    private static StoryDto AStoryTitled(string title) =>
        new(title, "https://example.com", "author", DateTimeOffset.UnixEpoch, 1, 0);
}
