using System.Text;
using System.Text.Json;

namespace BestStories.Api.IntegrationTests.TestSupport;

/// <summary>
/// Stands in for the Hacker News API. Programs one canned JSON body per upstream path and
/// records every path asked for, so a test can assert both the response and the upstream cost
/// of producing it. Unprogrammed paths return 404 rather than an empty body, so a test that
/// forgets to set one up fails loudly instead of silently.
/// </summary>
public sealed class HackerNewsStub : HttpMessageHandler
{
    private readonly Dictionary<string, string> jsonByPath = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> requestedPaths = [];
    private readonly Lock requestedPathsLock = new();

    public IReadOnlyList<string> RequestedPaths
    {
        get
        {
            lock (requestedPathsLock)
            {
                return requestedPaths.ToArray();
            }
        }
    }

    public int UpstreamCallCount => RequestedPaths.Count;

    public HackerNewsStub RespondsWithBestStoryIds(params int[] storyIds)
    {
        jsonByPath["beststories.json"] = JsonSerializer.Serialize(storyIds);
        return this;
    }

    public HackerNewsStub RespondsWithItem(int storyId, string itemJson)
    {
        jsonByPath[$"item/{storyId}.json"] = itemJson;
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var path = PathRelativeToApiRoot(request.RequestUri!);

        lock (requestedPathsLock)
        {
            requestedPaths.Add(path);
        }

        if (!jsonByPath.TryGetValue(path, out var json))
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
        }

        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
    }

    private static string PathRelativeToApiRoot(Uri requestUri)
    {
        var path = requestUri.AbsolutePath.TrimStart('/');

        return path.StartsWith("v0/", StringComparison.Ordinal) ? path["v0/".Length..] : path;
    }
}
