using Comments.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comments.Infrastructure.Persistence.Configurations;

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Kind).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.OriginalFileName).IsRequired().HasMaxLength(255);
        b.Property(x => x.StoredFileName).IsRequired().HasMaxLength(255);
        b.Property(x => x.ContentType).IsRequired().HasMaxLength(100);

        b.HasOne(x => x.Comment)
            .WithOne(c => c.Attachment)
            .HasForeignKey<Attachment>(x => x.CommentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}