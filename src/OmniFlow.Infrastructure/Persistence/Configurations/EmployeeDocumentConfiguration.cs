using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Persistence.Configurations;

public class EmployeeDocumentConfiguration : IEntityTypeConfiguration<EmployeeDocument>
{
    public void Configure(EntityTypeBuilder<EmployeeDocument> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Category)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.Title)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(e => e.FileName)
            .HasMaxLength(260)
            .IsRequired();

        builder.Property(e => e.FileStorageKey)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.ContentType)
            .HasMaxLength(128);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.TenantId, e.UserId });
    }
}
