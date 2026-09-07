namespace BestStories.Api.Stories;

public interface IStoryRequests
{
    DateTimeOffset LastRequestedAt { get; }

    void Record();
}
