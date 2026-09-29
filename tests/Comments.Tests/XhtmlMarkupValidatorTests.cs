using Comments.Infrastructure.Text;

namespace Comments.Tests;

public class XhtmlMarkupValidatorTests
{
    private readonly XhtmlMarkupValidator _sut = new();

    [Theory]
    [InlineData("plain text")]
    [InlineData("<i>italic</i>")]
    [InlineData("<strong>bold <i>and italic</i></strong>")]
    [InlineData("<a href=\"https://example.com\" title=\"x\">link</a>")]
    [InlineData("<code>var a = 1;</code>")]
    [InlineData("a < b and c > d")]
    [InlineData("<script>alert(1)</script>")] // запрещённый тег здесь не проверяем, его уберёт санитайзер
    public void Valid_markup_passes(string text) =>
        Assert.True(_sut.Validate(text).IsValid);

    [Theory]
    [InlineData("<i>never closed")]
    [InlineData("closing only</strong>")]
    [InlineData("<i><strong>wrong order</i></strong>")]
    [InlineData("<a href=\"https://example.com\">no end")]
    public void Invalid_markup_fails(string text)
    {
        var result = _sut.Validate(text);

        Assert.False(result.IsValid);
        Assert.NotNull(result.Error);
    }
}