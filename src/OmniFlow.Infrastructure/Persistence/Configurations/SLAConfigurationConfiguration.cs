using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Persistence.Configurations;

public class SLAConfigurationConfiguration : IEntityTypeConfiguration<SLAConfiguration>
{
    public void Configure(EntityTypeBuilder<SLAConfiguration> builder)
    {
        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.PriorityLevel).IsRequired().HasMaxLength(50);

        builder.HasOne(s => s.Department)
            .WithMany()
            .HasForeignKey(s => s.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
