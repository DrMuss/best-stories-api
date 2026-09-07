using BestStories.Api.HackerNews;
using BestStories.Api.Stories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BestStories.Api.IntegrationTests.TestSupport;

// The API booted in memory with the stub in place of the real upstream, so nothing in the
// integration suite reaches the network.
public sealed class ApiWithStubbedHackerNews(Dictionary<string, string?>? settings = null)
    : WebApplicationFactory<Program>
{
    public HackerNewsStub Upstream { get; } = new();

    // Serving starts before the first snapshot exists, so a test that wants stories waits for
    // the refresh the host kicked off rather than assuming it already happened.
    public async Task<HttpClient> CreateReadyClientAsync()
    {
        var client = CreateClient();
        await WaitUntilReadyAsync();

        return client;
    }

    public async Task WaitUntilReadyAsync()
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);

        while (!Services.GetRequiredService<IStorySnapshot>().Read().IsReady)
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("The story snapshot was never built.");
            }

            await Task.Delay(10);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Production backoff would have the suite waiting seconds per retry; the behaviour under
        // test is that a retry happens, not how long it politely waits first.
        var configured = new Dictionary<string, string?>
        {
            ["HackerNews:RetryDelay"] = "00:00:00.010",
            ["HackerNews:UpstreamAttemptTimeout"] = "00:00:01"
        };

        foreach (var setting in settings ?? [])
        {
            configured[setting.Key] = setting.Value;
        }

        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(configured));

        builder.ConfigureTestServices(services =>
            services.AddHttpClient<HackerNewsClient>(
                    httpClient => httpClient.BaseAddress = new Uri("https://hacker-news.test/v0/"))
                .ConfigurePrimaryHttpMessageHandler(() => Upstream));
    }
}
