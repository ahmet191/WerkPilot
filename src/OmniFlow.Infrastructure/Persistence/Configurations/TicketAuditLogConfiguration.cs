using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Persistence.Configurations;

public class TicketAuditLogConfiguration : IEntityTypeConfiguration<TicketAuditLog>
{
    public void Configure(EntityTypeBuilder<TicketAuditLog> builder)
    {
        builder.HasKey(al => al.Id);

        builder.Property(al => al.Action).IsRequired().HasMaxLength(50);
        builder.Property(al => al.FieldName).HasMaxLength(100);
        builder.Property(al => al.OldValue).HasMaxLength(1000);
        builder.Property(al => al.NewValue).HasMaxLength(1000);

        // Relationships
        builder.HasOne<Ticket>()
            .WithMany(t => t.AuditLogs)
            .HasForeignKey(al => al.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(al => al.ChangedBy)
            .WithMany()
            .HasForeignKey(al => al.ChangedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
