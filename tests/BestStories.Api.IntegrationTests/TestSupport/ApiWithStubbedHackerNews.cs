using BestStories.Api.HackerNews;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace BestStories.Api.IntegrationTests.TestSupport;

/// <summary>
/// The API booted in memory with <see cref="HackerNewsStub"/> in place of the real upstream.
/// Nothing in the integration suite reaches the network.
/// </summary>
public sealed class ApiWithStubbedHackerNews : WebApplicationFactory<Program>
{
    public HackerNewsStub Upstream { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(services =>
            services.AddHttpClient<HackerNewsClient>(
                    httpClient => httpClient.BaseAddress = new Uri("https://hacker-news.test/v0/"))
                .ConfigurePrimaryHttpMessageHandler(() => Upstream));
}
