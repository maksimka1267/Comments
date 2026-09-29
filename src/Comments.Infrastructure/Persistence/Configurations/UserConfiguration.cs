using Comments.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comments.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.UserName).IsRequired().HasMaxLength(50);
        b.Property(x => x.Email).IsRequired().HasMaxLength(254);
        b.Property(x => x.HomePage).HasMaxLength(2048);

        b.HasIndex(x => new { x.UserName, x.Email }).IsUnique(); // поиск/создание пользователя, сортировка по имени
        b.HasIndex(x => x.Email);                                 // сортировка по e-mail
    }
}