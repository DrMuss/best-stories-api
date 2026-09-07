using System.Text;
using System.Text.Json;

namespace BestStories.Api.IntegrationTests.TestSupport;

// Stands in for the Hacker News API, recording every path asked for so a test can assert the
// upstream cost of a response as well as its content. An unprogrammed path answers 404 rather
// than an empty body, so a test that forgets to set one up fails loudly.
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

    public HackerNewsStub RespondsWithStory(
        int storyId,
        int score,
        string title = "A story",
        string by = "author",
        long time = 1570887781,
        int descendants = 0,
        string url = "https://example.com") =>
        RespondsWithItem(storyId, $$"""
            {"by":"{{by}}","descendants":{{descendants}},"id":{{storyId}},"score":{{score}},"time":{{time}},"title":"{{title}}","type":"story","url":"{{url}}"}
            """);

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
