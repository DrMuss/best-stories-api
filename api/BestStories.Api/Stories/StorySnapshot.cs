using BestStories.Api.Contracts;

namespace BestStories.Api.Stories;

public sealed class StorySnapshot : IStorySnapshot
{
    private StoryDto[] stories = [];

    // A reader takes the array reference and is done; a writer puts a different array in its
    // place. Nobody observes a half-built list, so the read path needs no lock.
    public IReadOnlyList<StoryDto> Current => Volatile.Read(ref stories);

    // Copied rather than stored, so the caller cannot alter a published snapshot afterwards.
    public void Replace(IReadOnlyList<StoryDto> stories) =>
        Volatile.Write(ref this.stories, stories.ToArray());
}
