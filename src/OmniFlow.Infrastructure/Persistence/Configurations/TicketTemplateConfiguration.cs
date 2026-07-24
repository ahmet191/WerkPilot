using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Common;
using OmniFlow.Domain.Entities;
using System.Text.Json;

namespace OmniFlow.Infrastructure.Persistence.Configurations;

public class TicketTemplateConfiguration : IEntityTypeConfiguration<TicketTemplate>
{
    public void Configure(EntityTypeBuilder<TicketTemplate> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.LocalizedName)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<MultiLingualString>(v, (JsonSerializerOptions?)null) ?? new MultiLingualString()
            );

        // Serialize FieldDefinitions list to JSONB
        builder.Property(t => t.Fields)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<FieldDefinition>>(v, (JsonSerializerOptions?)null) ?? new List<FieldDefinition>()
            );

        // Serialize WorkflowDefinition object to JSONB
        builder.Property(t => t.Workflow)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<WorkflowDefinition>(v, (JsonSerializerOptions?)null) ?? new WorkflowDefinition()
            );
    }
}
