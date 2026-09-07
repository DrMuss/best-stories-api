using BestStories.Api.Contracts;

namespace BestStories.Api.HackerNews;

// Nullable where the upstream field is optional: url is absent on Ask HN posts, and descendants
// is absent or null on some items.
public sealed record HackerNewsItem(
    int Id,
    string? By,
    int? Descendants,
    int Score,
    long Time,
    string? Title,
    string? Type,
    string? Url)
{
    public StoryDto ToStory() => new(
        Title ?? string.Empty,
        Url,
        By ?? string.Empty,
        DateTimeOffset.FromUnixTimeSeconds(Time),
        Score,
        Descendants ?? 0);
}
