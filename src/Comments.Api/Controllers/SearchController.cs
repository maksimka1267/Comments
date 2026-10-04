using Comments.Api.Contracts;
using Comments.Domain.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace Comments.Api.Controllers;

[ApiController]
[Route("api/comments/search")]
public sealed class SearchController(ICommentSearchIndex index) : ControllerBase
{
    public const int PageSize = 25;

    [HttpGet]
    public async Task<ActionResult<PagedResult<SearchHitDto>>> Search(
        [FromQuery] SearchCommentsQuery query, CancellationToken ct)
    {
        try
        {
            var result = await index.SearchAsync(query.Q.Trim(), query.Page, PageSize, ct);

            var items = result.Hits
                .Select(h => new SearchHitDto(h.Id, h.ParentId, h.UserName, h.Snippet, h.CreatedAt))
                .ToList();

            return Ok(new PagedResult<SearchHitDto>(
                items, query.Page, PageSize, (int)Math.Min(result.TotalCount, int.MaxValue)));
        }
        catch (SearchUnavailableException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new ProblemDetails { Title = "Search is temporarily unavailable." });
        }
    }
}