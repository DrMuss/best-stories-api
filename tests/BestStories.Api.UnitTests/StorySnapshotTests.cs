using BestStories.Api.Contracts;
using BestStories.Api.Stories;
using Shouldly;

namespace BestStories.Api.UnitTests;

public class StorySnapshotTests
{
    [Fact]
    public void Snapshot_IsEmpty_BeforeAnythingIsPublished()
    {
        new StorySnapshot().Current.ShouldBeEmpty();
    }

    [Fact]
    public void Snapshot_IsReplacedByReference_NotMutated()
    {
        var snapshot = new StorySnapshot();
        snapshot.Replace([AStoryTitled("First")]);

        var readerHoldingTheOldSnapshot = snapshot.Current;
        snapshot.Replace([AStoryTitled("Second")]);

        readerHoldingTheOldSnapshot.Single().Title.ShouldBe("First");
        snapshot.Current.Single().Title.ShouldBe("Second");
        snapshot.Current.ShouldNotBeSameAs(readerHoldingTheOldSnapshot);
    }

    [Fact]
    public void Snapshot_IgnoresLaterChangesToTheListItWasGiven()
    {
        var snapshot = new StorySnapshot();
        var published = new[] { AStoryTitled("First") };

        snapshot.Replace(published);
        published[0] = AStoryTitled("Swapped underneath");

        snapshot.Current.Single().Title.ShouldBe("First");
    }

    private static StoryDto AStoryTitled(string title) =>
        new(title, "https://example.com", "author", DateTimeOffset.UnixEpoch, 1, 0);
}
