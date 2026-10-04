namespace Comments.Domain.Entities;

public class User
{
    private User() { } // для EF Core

    public User(string userName, string email, string? homePage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        UserName = userName;
        Email = email;
        HomePage = string.IsNullOrWhiteSpace(homePage) ? null : homePage;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string UserName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string? HomePage { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public ICollection<Comment> Comments { get; private set; } = new List<Comment>();

    public void UpdateHomePage(string? homePage) =>
        HomePage = string.IsNullOrWhiteSpace(homePage) ? null : homePage;
}