using System.ComponentModel.DataAnnotations;
using BestStories.Api.Contracts;
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

    private static Results<Ok<IReadOnlyList<StoryDto>>, ProblemHttpResult> GetStories(
        HttpContext httpContext,
        // Text rather than int?, so that the unparseable values are answered by the guard below
        // alongside every other invalid n, instead of by parameter binding with a different body.
        // [Required] reaches OpenAPI only: without it the API reference treats n as optional.
        [Required] string? n,
        IStorySnapshot snapshot)
    {
        if (RequestedStoryCount.Parse(n) is not int requestedCount)
        {
            return TypedResults.Problem(
                title: "Invalid story count",
                detail: "n is required and must be a positive integer.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (snapshot.Read() is not { IsReady: true, Stories: var stories })
        {
            // Seconds away, not minutes: a caller that waits gets an answer rather than a
            // permanently empty one.
            httpContext.Response.Headers.RetryAfter = "5";

            return TypedResults.Problem(
                title: "Stories are not available yet",
                detail: "The service is still building its first snapshot of the best stories.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        // A caller asking for more than exists is not an error; the list length is a ceiling.
        return TypedResults.Ok<IReadOnlyList<StoryDto>>(stories.Take(requestedCount).ToArray());
    }
}
