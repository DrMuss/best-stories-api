using BestStories.Api.Contracts;
using BestStories.Api.HackerNews;

namespace BestStories.Api.Stories;

public static class StoryRanker
{
    public static IReadOnlyList<StoryDto> RankBestFirst(IEnumerable<HackerNewsItem?> items) =>
        items
            // Nulls are ids upstream no longer has. The type filter is defensive: nothing
            // documents beststories.json as carrying only stories.
            .OfType<HackerNewsItem>()
            .Where(item => item.Type == "story")
            // The ordering of beststories.json is undocumented, so score order is established
            // here rather than trusted. Ties break on id so that the same set of items always
            // serialises identically.
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Id)
            .Select(item => item.ToStory())
            .ToArray();
}
