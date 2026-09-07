using System.Net.Http.Json;

namespace BestStories.Api.HackerNews;

/// <summary>
/// The only code in the service that talks to Hacker News.
/// </summary>
public sealed class HackerNewsClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<int>> GetBestStoryIdsAsync(CancellationToken cancellationToken) =>
        await httpClient.GetFromJsonAsync<int[]>("beststories.json", cancellationToken) ?? [];

    // Hacker News answers an unknown id with 200 and a body of null, so the null is a real
    // outcome rather than an error, and the return type says so.
    public Task<HackerNewsItem?> GetItemAsync(int storyId, CancellationToken cancellationToken) =>
        httpClient.GetFromJsonAsync<HackerNewsItem>($"item/{storyId}.json", cancellationToken);
}
