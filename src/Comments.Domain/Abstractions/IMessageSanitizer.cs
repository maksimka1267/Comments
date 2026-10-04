namespace Comments.Domain.Abstractions;

public interface IMessageSanitizer
{
    string Sanitize(string text);
}