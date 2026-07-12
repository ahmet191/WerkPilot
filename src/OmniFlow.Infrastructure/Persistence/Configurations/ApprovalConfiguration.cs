using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities.Approvals;

namespace OmniFlow.Infrastructure.Persistence.Configurations;

public class ApprovalRequestConfiguration : IEntityTypeConfiguration<ApprovalRequest>
{
    public void Configure(EntityTypeBuilder<ApprovalRequest> builder)
    {
        builder.ToTable("ApprovalRequests");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.EntityType).HasMaxLength(64).IsRequired();
        builder.Property(a => a.EntityDisplay).HasMaxLength(256);

        // The lookup used by every consumer and the timeline view.
        builder.HasIndex(a => new { a.EntityType, a.EntityId });
        builder.HasIndex(a => a.Status);

        builder.HasMany(a => a.Steps)
            .WithOne(s => s.ApprovalRequest)
            .HasForeignKey(s => s.ApprovalRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ApprovalStepConfiguration : IEntityTypeConfiguration<ApprovalStep>
{
    public void Configure(EntityTypeBuilder<ApprovalStep> builder)
    {
        builder.ToTable("ApprovalSteps");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.ApproverRole).HasMaxLength(64);
        builder.Property(s => s.Comment).HasMaxLength(1000);

        // "My pending approvals" scans.
        builder.HasIndex(s => new { s.ApproverUserId, s.Status });
        builder.HasIndex(s => new { s.ApproverRole, s.Status });
    }
}
