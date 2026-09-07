using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BestStories.Api.Stories;

// Readiness is about whether this instance can answer, which is exactly whether the snapshot
// has been built. Liveness is separate: the process is running either way.
public sealed class StorySnapshotReadiness(IStorySnapshot snapshot) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(snapshot.Read().IsReady
            ? HealthCheckResult.Healthy("The story snapshot has been built.")
            : HealthCheckResult.Unhealthy("The story snapshot has not been built yet."));
}
