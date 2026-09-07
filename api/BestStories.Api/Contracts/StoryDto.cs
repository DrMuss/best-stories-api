using System.Text.Json.Serialization;

namespace BestStories.Api.Contracts;

/// <summary>
/// A single best story, in the shape the brief specifies.
/// </summary>
/// <param name="Title">The story title, passed through from Hacker News unmodified.</param>
/// <param name="Uri">The story link. Absent on Ask HN posts, which carry no url.</param>
/// <param name="PostedBy">The Hacker News username of the submitter.</param>
/// <param name="Time">Submission time, ISO-8601 with offset (2019-10-12T13:43:01+00:00), not Z.</param>
/// <param name="Score">The story score, which the ordering of the response is based on.</param>
/// <param name="CommentCount">Hacker News calls this descendants.</param>
public sealed record StoryDto(
    string Title,

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Uri,
    string PostedBy,
    DateTimeOffset Time,
    int Score,
    int CommentCount);
