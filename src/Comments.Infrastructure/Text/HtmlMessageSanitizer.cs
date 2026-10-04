using Comments.Domain.Abstractions;

using Ganss.Xss;

namespace Comments.Infrastructure.Text;

public sealed class HtmlMessageSanitizer : IMessageSanitizer
{
    private readonly HtmlSanitizer _sanitizer;

    public HtmlMessageSanitizer()
    {
        _sanitizer = new HtmlSanitizer();

        _sanitizer.AllowedTags.Clear();
        foreach (var tag in new[] { "a", "code", "i", "strong" })
            _sanitizer.AllowedTags.Add(tag);

        _sanitizer.AllowedAttributes.Clear();
        _sanitizer.AllowedAttributes.Add("href");
        _sanitizer.AllowedAttributes.Add("title");

        _sanitizer.AllowedSchemes.Clear();
        _sanitizer.AllowedSchemes.Add("http");
        _sanitizer.AllowedSchemes.Add("https");

        // запрещённые теги убираем, а текст внутри них оставляем
        _sanitizer.KeepChildNodes = true;
    }

    public string Sanitize(string text) => _sanitizer.Sanitize(text);
}