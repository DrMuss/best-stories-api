using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace BestStories.Api.IntegrationTests;

public class ApiReferenceTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Root_RedirectsToApiReference()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/");

        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        response.Headers.Location!.ToString().ShouldBe("/scalar");
    }

    [Fact]
    public async Task OpenApiDocument_IsServed()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
