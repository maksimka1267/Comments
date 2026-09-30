using Comments.Api.Contracts;
using Comments.Api.Validators;
using Comments.Infrastructure.Text;

namespace Comments.Tests;

public class CreateCommentRequestValidatorTests
{
    private readonly CreateCommentRequestValidator _sut = new(new XhtmlMarkupValidator());

    private static CreateCommentRequest Valid() =>
    new("User123", "user@example.com", null, "Hello <i>world</i>", null, Guid.NewGuid(), "AB3CD");

    [Fact]
    public void Empty_captcha_id_fails() =>
        Assert.False(_sut.Validate(Valid() with { CaptchaId = Guid.Empty }).IsValid);

    [Theory]
    [InlineData("")]
    [InlineData("ab cd")]
    [InlineData("капча")]
    public void Invalid_captcha_answer_fails(string answer) =>
        Assert.False(_sut.Validate(Valid() with { CaptchaAnswer = answer }).IsValid);

    [Fact]
    public void Valid_request_passes() =>
        Assert.True(_sut.Validate(Valid()).IsValid);

    [Theory]
    [InlineData("")]
    [InlineData("user name")]
    [InlineData("user_1")]
    [InlineData("юзер")]
    public void Invalid_user_name_fails(string name) =>
        Assert.False(_sut.Validate(Valid() with { UserName = name }).IsValid);

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("user@")]
    public void Invalid_email_fails(string email) =>
        Assert.False(_sut.Validate(Valid() with { Email = email }).IsValid);

    [Theory]
    [InlineData("ftp://example.com")]
    [InlineData("not a url")]
    [InlineData("javascript:alert(1)")]
    public void Invalid_home_page_fails(string url) =>
        Assert.False(_sut.Validate(Valid() with { HomePage = url }).IsValid);

    [Fact]
    public void Empty_home_page_is_allowed() =>
        Assert.True(_sut.Validate(Valid() with { HomePage = "" }).IsValid);

    [Fact]
    public void Unclosed_tag_fails() =>
        Assert.False(_sut.Validate(Valid() with { Text = "<strong>oops" }).IsValid);
}