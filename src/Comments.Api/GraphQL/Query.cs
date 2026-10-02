using Comments.Api.Contracts;
using Comments.Api.Services;

using HotChocolate;

using Microsoft.Extensions.DependencyInjection;

namespace Comments.Api.GraphQL;

/// <summary>
/// Корневой тип GraphQL: те же данные, что и в REST, через тот же сервис (с кэшем).
/// </summary>
public sealed class Query
{
    /// <summary>Заглавные комментарии с сортировкой и пагинацией (по 25 на страницу).</summary>
    public async Task<PagedResult<CommentDto>> GetComments(
        [Service] IServiceScopeFactory scopes,
        CancellationToken ct,
        CommentSortField sortBy = CommentSortField.Date,
        SortDirection sortDir = SortDirection.Desc,
        int page = 1)
    {
        if (page < 1)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Page must be 1 or greater.")
                    .SetCode("INVALID_PAGE")
                    .Build());
        }

        // поля корневого типа выполняются параллельно, а DbContext не потокобезопасен,
        // поэтому каждый вызов работает в собственной области
        await using var scope = scopes.CreateAsyncScope();
        var queries = scope.ServiceProvider.GetRequiredService<ICommentQueryService>();

        return await queries.GetTopLevelAsync(
            new GetCommentsQuery { SortBy = sortBy, SortDir = sortDir, Page = page }, ct);
    }

    /// <summary>Один комментарий со всей веткой ответов; null, если такого нет.</summary>
    public async Task<CommentDto?> GetComment(
        [Service] IServiceScopeFactory scopes,
        CancellationToken ct,
        Guid id)
    {
        await using var scope = scopes.CreateAsyncScope();
        var queries = scope.ServiceProvider.GetRequiredService<ICommentQueryService>();

        return await queries.GetByIdAsync(id, ct);
    }
}