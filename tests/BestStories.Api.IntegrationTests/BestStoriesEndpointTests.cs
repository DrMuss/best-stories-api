using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace BestStories.Api.IntegrationTests;

public class BestStoriesEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetStories_ReturnsEmptyArray_WhenNoStoriesAvailable()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/stories");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("[]");
    }
}
