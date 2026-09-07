using BestStories.Api.HackerNews;
using Microsoft.Extensions.Options;

namespace BestStories.Api.Stories;

public sealed class StoryRefreshService(
    IStoryRefresher refresher,
    TimeProvider timeProvider,
    IOptions<HackerNewsOptions> options,
    IStoryRequests requests,
    ILogger<StoryRefreshService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.RefreshInterval, timeProvider);

        // A cycle first, then the wait, so the snapshot starts being built as the service does.
        // A cycle that outruns the interval delays the next rather than running alongside it,
        // and PeriodicTimer remembers at most one missed tick, so it cannot bank a backlog.
        do
        {
            if (NobodyHasAskedRecently())
            {
                logger.LogDebug("Skipping the story refresh; nothing has been served recently.");

                continue;
            }

            try
            {
                await refresher.RefreshAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception failure)
            {
                // The cycle is discarded whole, so the previous snapshot stands and callers see
                // stale stories rather than an outage. Letting this escape would end the loop
                // and leave the snapshot frozen with nothing saying why.
                logger.LogWarning(failure, "Story refresh failed; serving the previous snapshot.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private bool NobodyHasAskedRecently() =>
        timeProvider.GetUtcNow() - requests.LastRequestedAt > options.Value.IdleTimeout;
}
