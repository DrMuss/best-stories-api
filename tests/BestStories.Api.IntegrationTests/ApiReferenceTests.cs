using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BestStories.Api.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace BestStories.Api.IntegrationTests;

public class ApiReferenceTests
{
    [Fact]
    public async Task Root_RedirectsToApiReference()
    {
        using var api = new ApiWithStubbedHackerNews();
        var client = api.CreateClient(new WebApplicationFactoryClientOptions
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
        using var api = new ApiWithStubbedHackerNews();
        var client = api.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // Scalar leaves an optional query parameter switched off, so a caller who types a value
    // into n still sends a request without it. The type and minimum are the contract the
    // endpoint enforces, which the generated document cannot infer from a text parameter.
    [Fact]
    public async Task OpenApiDocument_DescribesNAsARequiredPositiveInteger()
    {
        using var api = new ApiWithStubbedHackerNews();
        var client = api.CreateClient();

        var document = await client.GetFromJsonAsync<JsonDocument>("/openapi/v1.json");

        var n = document!.RootElement
            .GetProperty("paths")
            .GetProperty("/stories")
            .GetProperty("get")
            .GetProperty("parameters")
            .EnumerateArray()
            .Single(parameter => parameter.GetProperty("name").GetString() == "n");

        n.GetProperty("required").GetBoolean().ShouldBeTrue();
        n.GetProperty("schema").GetProperty("type").GetString().ShouldBe("integer");
        n.GetProperty("schema").GetProperty("minimum").GetInt32().ShouldBe(1);
    }

    // The API reference builds its sample response from this example. Generated from the schema
    // alone it shows a Z-suffixed time, which is not the format the endpoint returns.
    [Fact]
    public async Task OpenApiDocument_ShowsAnExampleTimeWithOffset()
    {
        using var api = new ApiWithStubbedHackerNews();
        var client = api.CreateClient();

        var document = await client.GetFromJsonAsync<JsonDocument>("/openapi/v1.json");

        var example = document!.RootElement
            .GetProperty("components")
            .GetProperty("schemas")
            .GetProperty("StoryDto")
            .GetProperty("example");

        example.GetProperty("time").GetString().ShouldBe("2019-10-12T13:43:01+00:00");
    }
}
