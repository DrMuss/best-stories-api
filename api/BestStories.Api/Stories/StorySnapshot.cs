using BestStories.Api.Contracts;

namespace BestStories.Api.Stories;

public sealed class StorySnapshot : IStorySnapshot
{
    private StoryDto[]? stories;

    // Nobody can observe a half-built list, so the read path needs no lock.
    public SnapshotRead Read() =>
        Volatile.Read(ref stories) is { } published ? SnapshotRead.Of(published) : SnapshotRead.NotBuiltYet;

    // Copied rather than stored, so the caller cannot alter a published snapshot afterwards.
    public void Replace(IReadOnlyList<StoryDto> stories) =>
        Volatile.Write(ref this.stories, stories.ToArray());
}
