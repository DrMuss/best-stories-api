using BestStories.Api.HackerNews;
using Microsoft.Extensions.Options;

namespace BestStories.Api.Stories;

public sealed class StoryRefresher(
    IServiceScopeFactory scopeFactory,
    IStorySnapshot snapshot,
    IOptions<HackerNewsOptions> options) : IStoryRefresher
{
    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        // Resolved per cycle rather than captured: a typed client held by a singleton pins one
        // HttpMessageHandler for the life of the process, which is what the factory rotates.
        using var scope = scopeFactory.CreateScope();
        var hackerNews = scope.ServiceProvider.GetRequiredService<HackerNewsClient>();

        var bestStoryIds = await hackerNews.GetBestStoryIdsAsync(cancellationToken);

        // Every item: the top n by score cannot be identified without every score. Every id is
        // in flight as a task, but the semaphore decides how many of them are on the wire.
        using var fetchSlots = new SemaphoreSlim(options.Value.MaxConcurrentItemFetches);

        var items = await Task.WhenAll(bestStoryIds.Select(async storyId =>
        {
            await fetchSlots.WaitAsync(cancellationToken);

            try
            {
                return await hackerNews.GetItemAsync(storyId, cancellationToken);
            }
            finally
            {
                fetchSlots.Release();
            }
        }));

        snapshot.Replace(StoryRanker.RankBestFirst(items));
    }
}
