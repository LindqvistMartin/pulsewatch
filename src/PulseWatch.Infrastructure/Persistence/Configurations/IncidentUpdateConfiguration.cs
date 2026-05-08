using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PulseWatch.Core.Entities;

namespace PulseWatch.Infrastructure.Persistence.Configurations;

internal sealed class IncidentUpdateConfiguration : IEntityTypeConfiguration<IncidentUpdate>
{
    public void Configure(EntityTypeBuilder<IncidentUpdate> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>();
        builder.Property(x => x.Message).HasMaxLength(2000).IsRequired();
        builder.HasOne(x => x.Incident)
            .WithMany(x => x.Updates)
            .HasForeignKey(x => x.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
