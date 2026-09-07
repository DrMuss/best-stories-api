using BestStories.Api.Contracts;
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
    
    private static Ok<IReadOnlyList<StoryDto>> GetStories() =>
        TypedResults.Ok<IReadOnlyList<StoryDto>>([]);
}
