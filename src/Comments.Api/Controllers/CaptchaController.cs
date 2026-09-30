using Comments.Api.Contracts;
using Comments.Domain.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace Comments.Api.Controllers;

[ApiController]
[Route("api/captcha")]
public sealed class CaptchaController(ICaptchaService captcha) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CaptchaResponse>> Get(CancellationToken ct)
    {
        var challenge = await captcha.CreateAsync(ct);

        Response.Headers.CacheControl = "no-store";
        return new CaptchaResponse(
            challenge.Id,
            $"data:image/png;base64,{Convert.ToBase64String(challenge.Image)}");
    }
}