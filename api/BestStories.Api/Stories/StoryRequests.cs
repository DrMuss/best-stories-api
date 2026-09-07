namespace BestStories.Api.Stories;

public sealed class StoryRequests(TimeProvider timeProvider) : IStoryRequests
{
    // Starting counts as activity, so a service nobody has called yet still builds its first
    // snapshot rather than waiting for a request it would have to answer 503.
    private long lastRequestedAtTicks = timeProvider.GetUtcNow().UtcTicks;

    public DateTimeOffset LastRequestedAt =>
        new(Volatile.Read(ref lastRequestedAtTicks), TimeSpan.Zero);

    public void Record() => Volatile.Write(ref lastRequestedAtTicks, timeProvider.GetUtcNow().UtcTicks);
}
