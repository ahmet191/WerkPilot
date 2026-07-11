using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Persistence.Configurations;

public class TicketCommentConfiguration : IEntityTypeConfiguration<TicketComment>
{
    public void Configure(EntityTypeBuilder<TicketComment> builder)
    {
        builder.HasKey(tc => tc.Id);

        builder.Property(tc => tc.Content).IsRequired().HasMaxLength(2000);

        // Relationships
        builder.HasOne<Ticket>()
            .WithMany(t => t.Comments)
            .HasForeignKey(tc => tc.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tc => tc.Author)
            .WithMany()
            .HasForeignKey(tc => tc.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
