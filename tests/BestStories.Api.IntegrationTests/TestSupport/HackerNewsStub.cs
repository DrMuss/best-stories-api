using System.Text;
using System.Text.Json;

namespace BestStories.Api.IntegrationTests.TestSupport;

// Stands in for the Hacker News API, recording every path asked for so a test can assert the
// upstream cost of a response as well as its content. An unprogrammed path answers 404 rather
// than an empty body, so a test that forgets to set one up fails loudly.
public sealed class HackerNewsStub : HttpMessageHandler
{
    // An upstream offering no best stories: the baseline a test does not have to state.
    private readonly Dictionary<string, string> jsonByPath =
        new(StringComparer.OrdinalIgnoreCase) { ["beststories.json"] = "[]" };
    private readonly List<string> requestedPaths = [];
    private readonly Lock requestedPathsLock = new();
    private TaskCompletionSource? held;
    private int requestsInFlight;
    private int peakConcurrentRequests;

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

    public int PeakConcurrentRequests => Volatile.Read(ref peakConcurrentRequests);

    // Held requests stay open until released, so a test can see how many the caller is willing
    // to have in flight at once.
    public void HoldItemResponses() => Volatile.Write(ref held, new TaskCompletionSource());

    public void ReleaseHeldResponses()
    {
        var holding = Volatile.Read(ref held);
        Volatile.Write(ref held, null);
        holding?.SetResult();
    }

    public async Task WaitUntilRequestsInFlightReaches(int count)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);

        while (Volatile.Read(ref requestsInFlight) < count)
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException(
                    $"Only {Volatile.Read(ref requestsInFlight)} requests reached the stub, expected {count}.");
            }

            await Task.Delay(10);
        }
    }

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

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var path = PathRelativeToApiRoot(request.RequestUri!);

        lock (requestedPathsLock)
        {
            requestedPaths.Add(path);
        }

        RecordPeak(Interlocked.Increment(ref requestsInFlight));

        try
        {
            // Only item requests, so the list of ids that decides what to fetch still arrives.
            if (path.StartsWith("item/", StringComparison.Ordinal)
                && Volatile.Read(ref held) is { } holding)
            {
                await holding.Task.WaitAsync(cancellationToken);
            }

            if (!jsonByPath.TryGetValue(path, out var json))
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
            }

            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }
        finally
        {
            Interlocked.Decrement(ref requestsInFlight);
        }
    }

    private void RecordPeak(int inFlight)
    {
        var peak = Volatile.Read(ref peakConcurrentRequests);

        while (inFlight > peak)
        {
            var alreadyRecorded = Interlocked.CompareExchange(ref peakConcurrentRequests, inFlight, peak);

            if (alreadyRecorded == peak)
            {
                return;
            }

            peak = alreadyRecorded;
        }
    }

    private static string PathRelativeToApiRoot(Uri requestUri)
    {
        var path = requestUri.AbsolutePath.TrimStart('/');

        return path.StartsWith("v0/", StringComparison.Ordinal) ? path["v0/".Length..] : path;
    }
}
