namespace Comments.Api.Contracts;

public enum CommentSortField { Date, UserName, Email }

public enum SortDirection { Asc, Desc }

public sealed class GetCommentsQuery
{
    public CommentSortField SortBy { get; init; } = CommentSortField.Date;
    public SortDirection SortDir { get; init; } = SortDirection.Desc; // LIFO по умолчанию
}