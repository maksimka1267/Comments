using Comments.Api.Contracts;
using Comments.Api.Services;

using FluentValidation;

using Microsoft.AspNetCore.Mvc;

namespace Comments.Api.Controllers;

[ApiController]
[Route("api/comments")]
public sealed class CommentsController(
    ICommentService service,
    ICommentQueryService queries,
    IValidator<CreateCommentRequest> validator) : ControllerBase

{
    [HttpGet]
    public async Task<ActionResult<List<CommentDto>>> GetTopLevel(
        [FromQuery] GetCommentsQuery query, CancellationToken ct) =>
        Ok(await queries.GetTopLevelAsync(query, ct));

    [HttpPost]
    public async Task<IActionResult> Create(CreateCommentRequest request, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            return ValidationProblem(ModelState);
        }

        var client = new ClientInfo(
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers.UserAgent.ToString());

        var result = await service.CreateAsync(request, client, ct);

        switch (result.Status)
        {
            case CreateCommentStatus.Created:
                return StatusCode(StatusCodes.Status201Created, result.Comment);

            case CreateCommentStatus.ParentNotFound:
                return NotFound(new ProblemDetails { Title = "Parent comment not found." });

            default:
                ModelState.AddModelError(nameof(request.Text), "Text is empty after removing disallowed markup.");
                return ValidationProblem(ModelState);
        }
    }
}