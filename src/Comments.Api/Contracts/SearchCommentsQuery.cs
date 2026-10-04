using System.ComponentModel.DataAnnotations;

namespace Comments.Api.Contracts;

public sealed class SearchCommentsQuery
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Q { get; init; } = "";

    [Range(1, 400)]
    public int Page { get; init; } = 1;
}