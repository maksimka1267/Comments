using System.Net;
using System.Text.RegularExpressions;

namespace Comments.Infrastructure.Search;

public static partial class PlainText
{
    public static string FromHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        // сначала вырезаем теги, потом декодируем: закодированные &lt; и &gt; остаются обычным текстом
        var withoutTags = Tags().Replace(html, string.Empty);

        return Spaces().Replace(WebUtility.HtmlDecode(withoutTags), " ").Trim();
    }

    [GeneratedRegex("<[^>]*>")]
    private static partial Regex Tags();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();
}