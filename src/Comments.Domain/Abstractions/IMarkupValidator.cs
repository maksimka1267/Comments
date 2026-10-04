namespace Comments.Domain.Abstractions;

public interface IMarkupValidator
{
    MarkupValidationResult Validate(string text);
}

public sealed record MarkupValidationResult(bool IsValid, string? Error = null)
{
    public static MarkupValidationResult Ok() => new(true);
    public static MarkupValidationResult Fail(string error) => new(false, error);
}