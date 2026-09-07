using BestStories.Api.HackerNews;

namespace BestStories.Api.Stories;

public sealed class StoryRefresher(IServiceScopeFactory scopeFactory, IStorySnapshot snapshot) : IHostedService
{
    // The service does not begin serving until it holds a snapshot, so no caller sees an empty
    // list that only means "not fetched yet".
    public Task StartAsync(CancellationToken cancellationToken) => RefreshAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        // Resolved per cycle rather than captured: a typed client held by a singleton keeps one
        // HttpMessageHandler for the lifetime of the process, which is what the factory exists
        // to rotate.
        using var scope = scopeFactory.CreateScope();
        var hackerNews = scope.ServiceProvider.GetRequiredService<HackerNewsClient>();

        var bestStoryIds = await hackerNews.GetBestStoryIdsAsync(cancellationToken);

        // Every item, not the first n: the top n by score cannot be identified without every
        // score.
        var items = await Task.WhenAll(
            bestStoryIds.Select(storyId => hackerNews.GetItemAsync(storyId, cancellationToken)));

        snapshot.Replace(StoryRanker.RankBestFirst(items));
    }
}
