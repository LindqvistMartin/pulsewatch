using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PulseWatch.Core.Entities;

namespace PulseWatch.Infrastructure.Persistence.Configurations;

internal sealed class SloDefinitionConfiguration : IEntityTypeConfiguration<SloDefinition>
{
    public void Configure(EntityTypeBuilder<SloDefinition> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TargetAvailabilityPct).IsRequired();
        builder.Property(x => x.WindowDays).IsRequired();
        builder.HasOne(x => x.Probe)
            .WithMany(x => x.SloDefinitions)
            .HasForeignKey(x => x.ProbeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
