using System.Net;
using BestStories.Api.IntegrationTests.TestSupport;
using BestStories.Api.Stories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace BestStories.Api.IntegrationTests;

public class HealthEndpointTests
{
    [Fact]
    public async Task Health_ReportsHealthy()
    {
        using var api = new ApiWithStubbedHackerNews();
        var client = api.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Healthy");
    }

    [Fact]
    public async Task HealthProbes_DoNotCountAsUse()
    {
        using var api = new ApiWithStubbedHackerNews();
        var client = await api.CreateReadyClientAsync();

        var requests = api.Services.GetRequiredService<IStoryRequests>();
        var beforeTheProbes = requests.LastRequestedAt;

        await client.GetAsync("/health");
        await client.GetAsync("/health/ready");

        // An orchestrator probing every few seconds must not keep an unused instance refreshing.
        requests.LastRequestedAt.ShouldBe(beforeTheProbes);
    }
}
