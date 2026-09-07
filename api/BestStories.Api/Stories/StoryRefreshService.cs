using BestStories.Api.HackerNews;
using Microsoft.Extensions.Options;

namespace BestStories.Api.Stories;

public sealed class StoryRefreshService(
    IStoryRefresher refresher,
    TimeProvider timeProvider,
    IOptions<HackerNewsOptions> options,
    ILogger<StoryRefreshService> logger) : BackgroundService
{
    // The service does not begin serving until it holds a snapshot, so no caller sees an empty
    // list that only means "not fetched yet".
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await refresher.RefreshAsync(cancellationToken);
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.RefreshInterval, timeProvider);

        // A cycle that outruns the interval delays the next one rather than running alongside
        // it, and PeriodicTimer remembers at most one missed tick, so it cannot bank a backlog.
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
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
    }
}
