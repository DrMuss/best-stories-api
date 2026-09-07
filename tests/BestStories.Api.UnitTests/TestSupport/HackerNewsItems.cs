using BestStories.Api.HackerNews;

namespace BestStories.Api.UnitTests.TestSupport;

public static class HackerNewsItems
{
    public static HackerNewsItem AnItemWith(
        int id = 21233041,
        string? by = "ismaildonmez",
        int? descendants = 572,
        int score = 1716,
        long time = 1570887781,
        string? title = "A uBlock Origin update was rejected from the Chrome Web Store",
        string? type = "story",
        string? url = "https://github.com/uBlockOrigin/uBlock-issues/issues/745") =>
        new(id, by, descendants, score, time, title, type, url);
}
