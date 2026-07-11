using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Persistence.Configurations;

public class AppMenuConfiguration : IEntityTypeConfiguration<AppMenu>
{
    public void Configure(EntityTypeBuilder<AppMenu> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.TitleKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.Path)
            .HasMaxLength(200);

        builder.Property(m => m.Icon)
            .HasMaxLength(100);

        // Self-referencing hierarchical relationship
        builder.HasOne(m => m.Parent)
            .WithMany(m => m.Children)
            .HasForeignKey(m => m.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
