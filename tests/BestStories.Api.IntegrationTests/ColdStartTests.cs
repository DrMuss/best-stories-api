using System.Net;
using System.Net.Http.Json;
using BestStories.Api.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BestStories.Api.IntegrationTests;

public class ColdStartTests
{
    [Fact]
    public async Task Endpoint_Returns503WithRetryAfter_BeforeFirstRefresh()
    {
        using var api = AnApiStuckBuildingItsFirstSnapshot();
        var client = api.CreateClient();

        var response = await client.GetAsync("/stories?n=3");

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        response.Headers.RetryAfter!.Delta.ShouldBe(TimeSpan.FromSeconds(5));

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.ShouldBe("Stories are not available yet");
    }

    [Fact]
    public async Task Endpoint_StillRejectsAnInvalidN_BeforeFirstRefresh()
    {
        using var api = AnApiStuckBuildingItsFirstSnapshot();
        var client = api.CreateClient();

        var response = await client.GetAsync("/stories?n=0");

        // A bad request is a bad request whether or not we could have answered a good one.
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Readiness_ReportsNotReady_UntilFirstRefreshSucceeds()
    {
        using var api = AnApiStuckBuildingItsFirstSnapshot();
        var client = api.CreateClient();

        (await client.GetAsync("/health/ready")).StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);

        api.Upstream.ReleaseHeldResponses();
        await api.WaitUntilReadyAsync();

        (await client.GetAsync("/health/ready")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync("/stories?n=1")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Liveness_ReportsHealthy_WhileTheFirstSnapshotIsStillBeingBuilt()
    {
        using var api = AnApiStuckBuildingItsFirstSnapshot();
        var client = api.CreateClient();

        // The process is up and answering; it simply has nothing to serve yet. Reporting this
        // unhealthy would have an orchestrator restart a service that is starting normally.
        (await client.GetAsync("/health")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static ApiWithStubbedHackerNews AnApiStuckBuildingItsFirstSnapshot()
    {
        var api = new ApiWithStubbedHackerNews();

        api.Upstream
            .RespondsWithBestStoryIds(1, 2)
            .RespondsWithStory(1, score: 10)
            .RespondsWithStory(2, score: 20)
            .HoldItemResponses();

        return api;
    }
}
