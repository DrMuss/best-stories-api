using BestStories.Api.Contracts;
using BestStories.Api.HackerNews;
using BestStories.Api.Stories;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace BestStories.Api.UnitTests;

public class StoryRefreshServiceTests
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(1);

    [Fact]
    public async Task RefreshLoop_ReplacesSnapshot_AfterInterval()
    {
        var (service, refresher, clock, snapshot) = ARefreshService();

        await service.StartAsync(CancellationToken.None);
        snapshot.Current.Single().Title.ShouldBe("Cycle 1");

        await AdvanceUntilTheNextCycleCompletes(clock, refresher);
        snapshot.Current.Single().Title.ShouldBe("Cycle 2");

        await AdvanceUntilTheNextCycleCompletes(clock, refresher);
        snapshot.Current.Single().Title.ShouldBe("Cycle 3");

        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task RefreshCycles_DoNotOverlap_WhenRefreshIsSlow()
    {
        var (service, refresher, clock, _) = ARefreshService();
        await service.StartAsync(CancellationToken.None);

        refresher.MakeCyclesHang();
        await AdvanceUntilTheNextCycleStarts(clock, refresher);

        // However long the second cycle runs, and however many intervals elapse meanwhile,
        // no third cycle joins it.
        clock.Advance(RefreshInterval);
        clock.Advance(RefreshInterval);
        clock.Advance(RefreshInterval);

        (await refresher.CyclesStartedStayAt(2)).ShouldBeTrue();

        refresher.LetHangingCyclesFinish();
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task RefreshLoop_StopsCycling_WhenTheServiceStops()
    {
        var (service, refresher, clock, _) = ARefreshService();
        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        clock.Advance(RefreshInterval);
        clock.Advance(RefreshInterval);

        (await refresher.CyclesStartedStayAt(1)).ShouldBeTrue();
    }

    // The loop arms the timer again only after a cycle returns, so a single Advance can land in
    // the gap and be lost. Advancing until the loop responds is what "an interval elapsed"
    // means when the clock is not real.
    private static Task AdvanceUntilTheNextCycleCompletes(FakeTimeProvider clock, RecordingRefresher refresher)
    {
        var target = refresher.CyclesCompleted + 1;

        return AdvanceUntil(clock, () => refresher.CyclesCompleted >= target);
    }

    private static Task AdvanceUntilTheNextCycleStarts(FakeTimeProvider clock, RecordingRefresher refresher)
    {
        var target = refresher.CyclesStarted + 1;

        return AdvanceUntil(clock, () => refresher.CyclesStarted >= target);
    }

    private static async Task AdvanceUntil(FakeTimeProvider clock, Func<bool> cycled)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);

        while (!cycled())
        {
            (DateTime.UtcNow < deadline).ShouldBeTrue("Timed out waiting for the refresh loop.");
            clock.Advance(RefreshInterval);
            await Task.Delay(10);
        }
    }

    private static (StoryRefreshService, RecordingRefresher, FakeTimeProvider, IStorySnapshot) ARefreshService()
    {
        var snapshot = new StorySnapshot();
        var refresher = new RecordingRefresher(snapshot);
        var clock = new FakeTimeProvider();
        var options = Options.Create(new HackerNewsOptions
        {
            BaseUrl = "https://hacker-news.test/v0/",
            RefreshInterval = RefreshInterval
        });

        return (new StoryRefreshService(refresher, clock, options), refresher, clock, snapshot);
    }

    // Publishes a snapshot naming the cycle that produced it, so a test can tell one refresh
    // from the next.
    private sealed class RecordingRefresher(IStorySnapshot snapshot) : IStoryRefresher
    {
        private static readonly TimeSpan LongEnoughToBeSure = TimeSpan.FromMilliseconds(250);
        private TaskCompletionSource? hang;
        private int cyclesStarted;
        private int cyclesCompleted;

        public int CyclesStarted => Volatile.Read(ref cyclesStarted);

        public int CyclesCompleted => Volatile.Read(ref cyclesCompleted);

        public void MakeCyclesHang() => Volatile.Write(ref hang, new TaskCompletionSource());

        public void LetHangingCyclesFinish()
        {
            var hanging = Volatile.Read(ref hang);
            Volatile.Write(ref hang, null);
            hanging?.SetResult();
        }

        // The loop runs on its own task, so proving that nothing happened means watching for a
        // while rather than reading the counter once.
        public async Task<bool> CyclesStartedStayAt(int count)
        {
            var until = DateTime.UtcNow + LongEnoughToBeSure;

            while (DateTime.UtcNow < until)
            {
                if (CyclesStarted != count)
                {
                    return false;
                }

                await Task.Delay(10);
            }

            return true;
        }

        public async Task RefreshAsync(CancellationToken cancellationToken)
        {
            var cycle = Interlocked.Increment(ref cyclesStarted);

            if (Volatile.Read(ref hang) is { } hanging)
            {
                await hanging.Task.WaitAsync(cancellationToken);
            }

            snapshot.Replace([new StoryDto($"Cycle {cycle}", null, "author", DateTimeOffset.UnixEpoch, 1, 0)]);
            Interlocked.Increment(ref cyclesCompleted);
        }
    }
}
