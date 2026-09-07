using System.Net;
using BestStories.Api.IntegrationTests.TestSupport;
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
}
