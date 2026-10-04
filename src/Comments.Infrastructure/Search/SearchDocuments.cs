using Comments.Domain.Abstractions;
using Comments.Domain.Events;

namespace Comments.Infrastructure.Search;

public static class SearchDocuments
{
    public static CommentSearchDocument FromEvent(CommentCreatedEvent e) =>
        new(e.CommentId, e.ParentId, e.UserName, PlainText.FromHtml(e.Text), e.CreatedAt);
}