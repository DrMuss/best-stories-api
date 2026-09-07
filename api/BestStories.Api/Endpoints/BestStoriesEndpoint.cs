using System.ComponentModel.DataAnnotations;
using BestStories.Api.Contracts;
using BestStories.Api.HackerNews;
using BestStories.Api.Stories;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BestStories.Api.Endpoints;

public static class BestStoriesEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/stories", GetStories)
            .WithName("GetBestStories")
            .WithSummary("Returns the best n stories in descending order of score.");

        return endpoints;
    }

    private static async Task<Results<Ok<IReadOnlyList<StoryDto>>, ProblemHttpResult>> GetStories(
        // Taken as text and parsed here so that every way of getting n wrong — missing, zero,
        // negative, not a number, or too large to be an int — gets the same answer. Left as
        // int? the framework would answer the unparseable ones itself, with a different body.
        // [Required] only describes the parameter to OpenAPI — without it the API reference
        // shows n as optional and sends requests without it. The guard below does the work.
        [Required] string? n,
        HackerNewsClient hackerNews,
        CancellationToken cancellationToken)
    {
        if (RequestedStoryCount.Parse(n) is not int requestedCount)
        {
            return TypedResults.Problem(
                title: "Invalid story count",
                detail: "n is required and must be a positive integer.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var bestStoryIds = await hackerNews.GetBestStoryIdsAsync(cancellationToken);

        // Every item is fetched, not the first n: the top n by score cannot be identified
        // without every score.
        var items = await Task.WhenAll(
            bestStoryIds.Select(storyId => hackerNews.GetItemAsync(storyId, cancellationToken)));

        var bestStories = StoryRanker.RankBestFirst(items);

        // A caller asking for more than exists is not an error; the list length is a ceiling.
        return TypedResults.Ok<IReadOnlyList<StoryDto>>(bestStories.Take(requestedCount).ToArray());
    }
}
