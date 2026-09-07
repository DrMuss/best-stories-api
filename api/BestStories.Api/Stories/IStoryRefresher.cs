namespace BestStories.Api.Stories;

public interface IStoryRefresher
{
    Task RefreshAsync(CancellationToken cancellationToken);
}
