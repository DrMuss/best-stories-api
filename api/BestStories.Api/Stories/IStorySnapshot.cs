using BestStories.Api.Contracts;

namespace BestStories.Api.Stories;

public interface IStorySnapshot
{
    SnapshotRead Read();

    void Replace(IReadOnlyList<StoryDto> stories);
}
