using BestStories.Api.Contracts;

namespace BestStories.Api.Stories;

public interface IStorySnapshot
{
    IReadOnlyList<StoryDto> Current { get; }

    void Replace(IReadOnlyList<StoryDto> stories);
}
