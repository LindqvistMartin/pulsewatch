using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PulseWatch.Core.Entities;

namespace PulseWatch.Infrastructure.Persistence.Configurations;

internal sealed class HealthCheckConfiguration : IEntityTypeConfiguration<HealthCheck>
{
    public void Configure(EntityTypeBuilder<HealthCheck> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FailureReason).HasMaxLength(1000);
        builder.HasIndex(x => new { x.ProbeId, x.CheckedAt });
        builder.HasOne(x => x.Probe)
            .WithMany(x => x.HealthChecks)
            .HasForeignKey(x => x.ProbeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
