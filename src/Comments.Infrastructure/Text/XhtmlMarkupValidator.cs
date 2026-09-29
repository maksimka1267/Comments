using System.Text.RegularExpressions;

using Comments.Domain.Abstractions;

namespace Comments.Infrastructure.Text;

public sealed partial class XhtmlMarkupValidator : IMarkupValidator
{
    private static readonly HashSet<string> AllowedTags =
        new(StringComparer.OrdinalIgnoreCase) { "a", "code", "i", "strong" };

    [GeneratedRegex(@"<\s*(?<close>/)?\s*(?<name>[a-zA-Z][a-zA-Z0-9]*)(?<attrs>[^<>]*?)(?<self>/)?\s*>")]
    private static partial Regex TagRegex();

    public MarkupValidationResult Validate(string text)
    {
        var stack = new Stack<string>();

        foreach (Match m in TagRegex().Matches(text))
        {
            var name = m.Groups["name"].Value.ToLowerInvariant();
            if (!AllowedTags.Contains(name))
                continue;

            var isClosing = m.Groups["close"].Success;
            var isSelfClosing = m.Groups["self"].Success;

            if (isClosing)
            {
                if (stack.Count == 0)
                    return MarkupValidationResult.Fail($"Unexpected closing tag </{name}>.");

                var open = stack.Pop();
                if (open != name)
                    return MarkupValidationResult.Fail($"Tag <{open}> must be closed before </{name}>.");
            }
            else if (!isSelfClosing)
            {
                stack.Push(name);
            }
        }

        return stack.Count == 0
            ? MarkupValidationResult.Ok()
            : MarkupValidationResult.Fail($"Tag <{stack.Peek()}> is not closed.");
    }
}