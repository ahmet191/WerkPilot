using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Persistence.Configurations;

public class SystemLogConfiguration : IEntityTypeConfiguration<SystemLog>
{
    public void Configure(EntityTypeBuilder<SystemLog> builder)
    {
        builder.HasKey(l => l.Id);
        
        builder.Property(l => l.Level).HasMaxLength(64).IsRequired();
        builder.Property(l => l.TraceId).HasMaxLength(128);

        builder.Property(l => l.Properties)
            .HasColumnType("jsonb")
            .IsRequired();

        // Index for sorting by date (pagination default)
        builder.HasIndex(l => l.Timestamp);
        
        // Index for querying specific traces
        builder.HasIndex(l => l.TraceId);
        
        // Index for multi-tenant filtering
        builder.HasIndex(l => l.TenantId);
        
        // Index for filtering by log level
        builder.HasIndex(l => l.Level);
    }
}
