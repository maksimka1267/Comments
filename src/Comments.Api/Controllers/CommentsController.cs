using Comments.Api.Contracts;
using Comments.Api.Services;
using Comments.Infrastructure.Files;

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
    private const int MaxRequestBytes = 6 * 1024 * 1024;
    private const int MaxImageBytes = 5 * 1024 * 1024;

    [HttpGet]
    public async Task<ActionResult<List<CommentDto>>> GetTopLevel(
        [FromQuery] GetCommentsQuery query, CancellationToken ct) =>
        Ok(await queries.GetTopLevelAsync(query, ct));

    [HttpPost]
    [RequestSizeLimit(MaxRequestBytes)]
    public async Task<IActionResult> Create(
    [FromForm] CreateCommentRequest request, IFormFile? file, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            return ValidationProblem(ModelState);
        }

        UploadedFile? upload = null;
        if (file is { Length: > 0 })
        {
            // размер проверяем до чтения в память
            var isText = Path.GetExtension(file.FileName).Equals(".txt", StringComparison.OrdinalIgnoreCase);
            var limit = isText ? TextFileProcessor.MaxBytes : MaxImageBytes;
            if (file.Length > limit)
            {
                ModelState.AddModelError("file", $"File must not exceed {limit / 1024} KB.");
                return ValidationProblem(ModelState);
            }

            using var buffer = new MemoryStream((int)file.Length);
            await file.CopyToAsync(buffer, ct);
            upload = new UploadedFile(file.FileName, buffer.ToArray());
        }

        var client = new ClientInfo(
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers.UserAgent.ToString());

        var result = await service.CreateAsync(request, upload, client, ct);

        switch (result.Status)
        {
            case CreateCommentStatus.Created:
                return CreatedAtAction(nameof(GetById), new { id = result.Comment!.Id }, result.Comment);

            case CreateCommentStatus.ParentNotFound:
                return NotFound(new ProblemDetails { Title = "Parent comment not found." });

            case CreateCommentStatus.InvalidCaptcha:
                ModelState.AddModelError(nameof(request.CaptchaAnswer), "Wrong or expired CAPTCHA.");
                return ValidationProblem(ModelState);

            case CreateCommentStatus.InvalidFile:
                ModelState.AddModelError("file", result.Error!);
                return ValidationProblem(ModelState);

            default:
                ModelState.AddModelError(nameof(request.Text), "Text is empty after removing disallowed markup.");
                return ValidationProblem(ModelState);
        }
    }
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CommentDto>> GetById(Guid id, CancellationToken ct)
    {
        var comment = await queries.GetByIdAsync(id, ct);
        return comment is null ? NotFound() : Ok(comment);
    }
}