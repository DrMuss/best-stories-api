using BestStories.Api.Contracts;

namespace BestStories.Api.Stories;

// A snapshot that has not been built yet is an operating condition, not a fault, so it is a
// value the caller handles rather than an exception it has to catch. Empty and not-yet-built
// are different answers: one is what Hacker News offered, the other is that we have not asked.
public readonly record struct SnapshotRead(bool IsReady, IReadOnlyList<StoryDto> Stories)
{
    public static SnapshotRead NotBuiltYet { get; } = new(false, []);

    public static SnapshotRead Of(IReadOnlyList<StoryDto> stories) => new(true, stories);
}
