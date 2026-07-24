using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Common;
using OmniFlow.Domain.Entities;
using System.Text.Json;

namespace OmniFlow.Infrastructure.Persistence.Configurations;

public class KnowledgeBaseArticleConfiguration : IEntityTypeConfiguration<KnowledgeBaseArticle>
{
    public void Configure(EntityTypeBuilder<KnowledgeBaseArticle> builder)
    {
        builder.HasKey(k => k.Id);

        builder.Property(k => k.LocalizedTitle)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<MultiLingualString>(v, (JsonSerializerOptions?)null) ?? new MultiLingualString()
            );

        builder.Property(k => k.LocalizedContent)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<MultiLingualString>(v, (JsonSerializerOptions?)null) ?? new MultiLingualString()
            );

        builder.HasOne(k => k.Department)
            .WithMany()
            .HasForeignKey(k => k.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(k => k.Category)
            .WithMany()
            .HasForeignKey(k => k.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
