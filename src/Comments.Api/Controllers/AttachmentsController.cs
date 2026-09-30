using Comments.Api.Services;
using Comments.Domain.Entities;

using Microsoft.AspNetCore.Mvc;

namespace Comments.Api.Controllers;

[ApiController]
[Route("api/attachments")]
public sealed class AttachmentsController(IAttachmentService attachments) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var file = await attachments.GetAsync(id, ct);
        if (file is null)
            return NotFound();

        // браузер не должен «угадывать» тип и исполнять загруженный файл как страницу или скрипт
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Content-Security-Policy"] = "default-src 'none'; sandbox";

        // имя файла в адресе — Guid и не меняется, поэтому кэшируем надолго
        Response.Headers.CacheControl = "public, max-age=31536000, immutable";

        var contentType = file.Kind == AttachmentKind.Text
            ? "text/plain; charset=utf-8"
            : file.ContentType;

        return File(file.Content, contentType);
    }
}