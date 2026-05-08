using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PulseWatch.Core.Entities;

namespace PulseWatch.Infrastructure.Persistence.Configurations;

internal sealed class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.ProbeId, x.ClosedAt });
        builder.HasOne(x => x.Probe)
            .WithMany(x => x.Incidents)
            .HasForeignKey(x => x.ProbeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
