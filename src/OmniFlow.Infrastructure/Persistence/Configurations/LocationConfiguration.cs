using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Persistence.Configurations;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.HasKey(l => l.Id);
        
        builder.Property(l => l.Name).IsRequired().HasMaxLength(200);
        builder.Property(l => l.Country).IsRequired().HasMaxLength(100);
        builder.Property(l => l.City).IsRequired().HasMaxLength(100);
        builder.Property(l => l.Address).HasMaxLength(1000);
        builder.Property(l => l.TimeZone).HasMaxLength(100);
    }
}
