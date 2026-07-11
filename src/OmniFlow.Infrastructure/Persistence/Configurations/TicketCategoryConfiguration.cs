using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Common;
using OmniFlow.Domain.Entities;
using System.Text.Json;

namespace OmniFlow.Infrastructure.Persistence.Configurations;

public class TicketCategoryConfiguration : IEntityTypeConfiguration<TicketCategory>
{
    public void Configure(EntityTypeBuilder<TicketCategory> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.LocalizedName)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<MultiLingualString>(v, (JsonSerializerOptions?)null) ?? new MultiLingualString()
            );

        builder.HasOne(t => t.Department)
            .WithMany()
            .HasForeignKey(t => t.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ParentCategory)
            .WithMany(t => t.SubCategories)
            .HasForeignKey(t => t.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
