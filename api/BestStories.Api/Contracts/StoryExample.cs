namespace BestStories.Api.Contracts;

public static class StoryExample
{
    public static readonly StoryDto Story = new(
        "A uBlock Origin update was rejected from the Chrome Web Store",
        "https://github.com/uBlockOrigin/uBlock-issues/issues/745",
        "ismaildonmez",
        new DateTimeOffset(2019, 10, 12, 13, 43, 1, TimeSpan.Zero),
        1716,
        572);
}
