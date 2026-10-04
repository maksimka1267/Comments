using System.Text.Json.Serialization;

using Comments.Domain.Abstractions;

namespace Comments.Infrastructure.Search;

public sealed class CommentIndexDocument
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("parentId")]
    public Guid? ParentId { get; init; }

    [JsonPropertyName("userName")]
    public string UserName { get; init; } = "";

    [JsonPropertyName("text")]
    public string Text { get; init; } = "";

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    public static CommentIndexDocument From(CommentSearchDocument document) => new()
    {
        Id = document.Id,
        ParentId = document.ParentId,
        UserName = document.UserName,
        Text = document.Text,
        CreatedAt = document.CreatedAt,
    };
}