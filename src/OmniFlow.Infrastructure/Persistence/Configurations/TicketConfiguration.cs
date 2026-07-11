using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;
using System.Text.Json;

namespace OmniFlow.Infrastructure.Persistence.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.HasKey(t => t.Id);

        // Map DynamicData to JSONB
        builder.Property(t => t.DynamicData)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>()
            );

        // Core properties
        builder.Property(t => t.Title).IsRequired().HasMaxLength(300);
        builder.Property(t => t.Description).HasMaxLength(5000);

        // One-to-Many Relationships
        builder.HasOne(t => t.Template)
            .WithMany()
            .HasForeignKey(t => t.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Requester)
            .WithMany()
            .HasForeignKey(t => t.RequesterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Assignee)
            .WithMany()
            .HasForeignKey(t => t.AssigneeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ServiceDesk)
            .WithMany(sd => sd.Tickets)
            .HasForeignKey(t => t.ServiceDeskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(t => t.ResolutionNotes).HasMaxLength(2000);

        builder.HasOne(t => t.ConfigurationItem)
            .WithMany()
            .HasForeignKey(t => t.ConfigurationItemId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.Problem)
            .WithMany(p => p.Incidents)
            .HasForeignKey(t => t.ProblemId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
