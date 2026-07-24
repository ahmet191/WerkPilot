using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities.HR;

namespace OmniFlow.Infrastructure.Persistence.Configurations.HR;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.Property(e => e.IsActive).HasDefaultValue(true);
        builder.HasOne(e => e.User)
               .WithOne()
               .HasForeignKey<Employee>(e => e.UserId)
               .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Department)
               .WithMany()
               .HasForeignKey(e => e.DepartmentId)
               .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(e => e.Position)
               .WithMany()
               .HasForeignKey(e => e.PositionId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
