using System.Globalization;

namespace BestStories.Api.Contracts;

public static class RequestedStoryCount
{
    // Every way of getting n wrong collapses to one null, so every one of them can be given the
    // same answer. Asking for more stories than exist is not one of them. Invariant culture
    // because "1.000" is a thousand to a German-cultured server and a malformed count to
    // everyone else.
    public static int? Parse(string? n) =>
        int.TryParse(n, NumberStyles.Integer, CultureInfo.InvariantCulture, out var requestedCount)
        && requestedCount > 0
            ? requestedCount
            : null;
}
