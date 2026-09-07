using BestStories.Api.Contracts;
using BestStories.Api.HackerNews;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BestStories.Api.Endpoints;

/// <summary>
/// GET /stories — the best stories by score.
/// </summary>
public static class BestStoriesEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/stories", GetStories)
            .WithName("GetBestStories")
            .WithSummary("Returns the best stories in descending order of score.");

        return endpoints;
    }

    private static async Task<Ok<IReadOnlyList<StoryDto>>> GetStories(
        int? n,
        HackerNewsClient hackerNews,
        CancellationToken cancellationToken)
    {
        var bestStoryIds = await hackerNews.GetBestStoryIdsAsync(cancellationToken);
        var requestedStoryIds = n is int requestedCount ? bestStoryIds.Take(requestedCount) : bestStoryIds;

        var stories = new List<StoryDto>();

        foreach (var storyId in requestedStoryIds)
        {
            var item = await hackerNews.GetItemAsync(storyId, cancellationToken);

            if (item is not null)
            {
                stories.Add(item.ToStory());
            }
        }

        return TypedResults.Ok<IReadOnlyList<StoryDto>>(stories);
    }
}
