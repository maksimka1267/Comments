using Comments.Api.Contracts;
using Comments.Domain.Abstractions;

using FluentValidation;

namespace Comments.Api.Validators;

public sealed class CreateCommentRequestValidator : AbstractValidator<CreateCommentRequest>
{
    public CreateCommentRequestValidator(IMarkupValidator markupValidator)
    {
        RuleFor(x => x.UserName)
            .NotEmpty()
            .MaximumLength(50)
            .Matches("^[A-Za-z0-9]+$")
            .WithMessage("User name may contain only Latin letters and digits.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(254)
            .EmailAddress();

        RuleFor(x => x.HomePage)
            .MaximumLength(2048)
            .Must(BeHttpUrl)
            .WithMessage("Home page must be a valid http(s) URL.")
            .When(x => !string.IsNullOrWhiteSpace(x.HomePage));

        RuleFor(x => x.Text)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(5000)
            .Custom((text, context) =>
            {
                var result = markupValidator.Validate(text);
                if (!result.IsValid)
                    context.AddFailure(result.Error!);
            });
    }

    private static bool BeHttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}