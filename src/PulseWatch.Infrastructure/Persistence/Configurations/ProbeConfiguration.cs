using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PulseWatch.Core.Entities;

namespace PulseWatch.Infrastructure.Persistence.Configurations;

internal sealed class ProbeConfiguration : IEntityTypeConfiguration<Probe>
{
    public void Configure(EntityTypeBuilder<Probe> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Url).HasMaxLength(2048).IsRequired();
        builder.Property(x => x.Method).HasMaxLength(10).IsRequired();
        builder.HasOne(x => x.Project)
            .WithMany(x => x.Probes)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
