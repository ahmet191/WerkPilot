using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Persistence.Configurations;

public class EmploymentDetailsConfiguration : IEntityTypeConfiguration<EmploymentDetails>
{
    public void Configure(EntityTypeBuilder<EmploymentDetails> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // Assuming 1:1 for simplicity right now
        builder.HasIndex(e => e.UserId).IsUnique();
    }
}
