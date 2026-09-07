using System.ComponentModel.DataAnnotations;
using System.Globalization;
using BestStories.Api.Contracts;
using BestStories.Api.Stories;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BestStories.Api.Endpoints;

public static class BestStoriesEndpoint
{
    // Advisory, and deliberately optimistic: a cold start is seconds. Guessing low costs the
    // caller one cheap retry, guessing high leaves them waiting long after we could answer.
    private static readonly TimeSpan RetryAfterWhileWarmingUp = TimeSpan.FromSeconds(5);

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
        IStorySnapshot snapshot,
        IStoryRequests requests)
    {
        if (RequestedStoryCount.Parse(n) is not int requestedCount)
        {
            return TypedResults.Problem(
                title: "Invalid story count",
                detail: "n is required and must be a positive integer.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        // Recorded here rather than in middleware: a health probe every few seconds is not
        // somebody reading stories, and would keep the refresh running forever.
        requests.Record();

        if (snapshot.Read() is not { IsReady: true, Stories: var stories })
        {
            httpContext.Response.Headers.RetryAfter =
                ((int)RetryAfterWhileWarmingUp.TotalSeconds).ToString(CultureInfo.InvariantCulture);

            return TypedResults.Problem(
                title: "Stories are not available yet",
                detail: "The service is still building its first snapshot of the best stories.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        // A caller asking for more than exists is not an error; the list length is a ceiling.
        return TypedResults.Ok<IReadOnlyList<StoryDto>>(stories.Take(requestedCount).ToArray());
    }
}
