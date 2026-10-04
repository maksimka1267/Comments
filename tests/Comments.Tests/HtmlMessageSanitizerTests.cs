using Comments.Infrastructure.Text;

namespace Comments.Tests;

public class HtmlMessageSanitizerTests
{
    private readonly HtmlMessageSanitizer _sut = new();

    [Fact]
    public void Allowed_tags_are_preserved()
    {
        Assert.Equal("<strong>a</strong> <i>b</i> <code>c</code>",
            _sut.Sanitize("<strong>a</strong> <i>b</i> <code>c</code>"));
    }

    [Fact]
    public void Link_keeps_href_and_title()
    {
        var result = _sut.Sanitize("<a href=\"https://example.com\" title=\"t\">x</a>");

        Assert.Contains("href=\"https://example.com\"", result);
        Assert.Contains("title=\"t\"", result);
    }

    [Fact]
    public void Script_tag_is_removed()
    {
        var result = _sut.Sanitize("<script>alert(1)</script>hello");

        Assert.DoesNotContain("<script", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hello", result);
    }

    [Fact]
    public void Javascript_scheme_is_removed()
    {
        var result = _sut.Sanitize("<a href=\"javascript:alert(1)\">x</a>");

        Assert.DoesNotContain("javascript", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Event_handlers_and_styles_are_removed()
    {
        var result = _sut.Sanitize("<i onclick=\"steal()\" style=\"color:red\">t</i>");

        Assert.Equal("<i>t</i>", result);
    }

    [Fact]
    public void Disallowed_tags_are_stripped_but_text_stays()
    {
        Assert.Equal("text", _sut.Sanitize("<div><img src=\"x\">text</div>"));
    }
}