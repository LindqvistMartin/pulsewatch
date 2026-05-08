using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PulseWatch.Core.Entities;

namespace PulseWatch.Infrastructure.Persistence.Configurations;

internal sealed class SloMeasurementConfiguration : IEntityTypeConfiguration<SloMeasurement>
{
    public void Configure(EntityTypeBuilder<SloMeasurement> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.SloDefinitionId, x.ComputedAt });
        builder.HasOne(x => x.SloDefinition)
            .WithMany(x => x.Measurements)
            .HasForeignKey(x => x.SloDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
