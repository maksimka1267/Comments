using Comments.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comments.Infrastructure.Persistence.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Text).IsRequired().HasMaxLength(5000);
        b.Property(x => x.IpAddress).IsRequired().HasMaxLength(45);   // хватает для IPv6
        b.Property(x => x.UserAgent).IsRequired().HasMaxLength(512);

        b.HasOne(x => x.User)
            .WithMany(u => u.Comments)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Parent)
            .WithMany(x => x.Replies)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // ответы конкретного комментария в порядке добавления
        b.HasIndex(x => new { x.ParentId, x.CreatedAt });

        // главная страница: только заглавные комментарии, сортировка по дате
        b.HasIndex(x => x.CreatedAt).HasFilter("\"ParentId\" IS NULL");
    }
}