using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Persistence.Configurations;

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.HasKey(a => a.Id);
        
        builder.Property(a => a.AssetTag).IsRequired().HasMaxLength(100);
        builder.Property(a => a.SerialNumber).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Model).HasMaxLength(200);
        builder.Property(a => a.Category).HasMaxLength(100);

        builder.HasIndex(a => a.AssetTag).IsUnique();
        builder.HasIndex(a => a.SerialNumber);

        builder.HasOne(a => a.AssignedToUser)
            .WithMany()
            .HasForeignKey(a => a.AssignedToUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
