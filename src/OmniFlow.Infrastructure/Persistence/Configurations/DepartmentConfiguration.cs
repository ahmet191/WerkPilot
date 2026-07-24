using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Common;
using OmniFlow.Domain.Entities;
using System.Text.Json;

namespace OmniFlow.Infrastructure.Persistence.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.HasKey(d => d.Id);

        // JSONB MultiLingualString Configuration
        builder.Property(d => d.LocalizedName)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<MultiLingualString>(v, (JsonSerializerOptions?)null) ?? new MultiLingualString()
            );

        // Relationships
        builder.HasOne(d => d.ParentDepartment)
            .WithMany(d => d.ChildDepartments)
            .HasForeignKey(d => d.ParentDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(d => d.Manager)
            .WithMany()
            .HasForeignKey(d => d.ManagerId)
            .OnDelete(DeleteBehavior.SetNull);

        // Department.Head is a second, distinct relationship to a person (an HR
        // Employee, vs Manager which is a User). Bind it explicitly to HeadId —
        // otherwise EF convention can't tell it apart from Employee.DepartmentId
        // and backs it with a shadow "HeadId1" column.
        builder.HasOne(d => d.Head)
            .WithMany()
            .HasForeignKey(d => d.HeadId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
