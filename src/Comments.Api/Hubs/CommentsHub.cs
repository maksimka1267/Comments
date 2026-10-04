using Comments.Api.Contracts;

using Microsoft.AspNetCore.SignalR;

namespace Comments.Api.Hubs;

/// <summary>Методы, которые сервер вызывает у браузеров.</summary>
public interface ICommentsClient
{
    Task CommentCreated(CommentCreatedNotification notification);
}

/// <summary>Хаб только для рассылки с сервера; клиентских методов нет.</summary>
public sealed class CommentsHub : Hub<ICommentsClient>
{
    public const string Route = "/hubs/comments";
}