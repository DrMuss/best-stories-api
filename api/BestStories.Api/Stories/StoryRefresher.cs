using BestStories.Api.HackerNews;

namespace BestStories.Api.Stories;

public sealed class StoryRefresher(IServiceScopeFactory scopeFactory, IStorySnapshot snapshot) : IStoryRefresher
{
    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        // Resolved per cycle rather than captured: a typed client held by a singleton pins one
        // HttpMessageHandler for the life of the process, which is what the factory rotates.
        using var scope = scopeFactory.CreateScope();
        var hackerNews = scope.ServiceProvider.GetRequiredService<HackerNewsClient>();

        var bestStoryIds = await hackerNews.GetBestStoryIdsAsync(cancellationToken);

        // Every item: the top n by score cannot be identified without every score.
        var items = await Task.WhenAll(
            bestStoryIds.Select(storyId => hackerNews.GetItemAsync(storyId, cancellationToken)));

        snapshot.Replace(StoryRanker.RankBestFirst(items));
    }
}
