using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PulseWatch.Core.Entities;

namespace PulseWatch.Infrastructure.Persistence.Configurations;

internal sealed class ProbeAssertionConfiguration : IEntityTypeConfiguration<ProbeAssertion>
{
    public void Configure(EntityTypeBuilder<ProbeAssertion> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ExpectedValue).HasMaxLength(500).IsRequired();
        builder.Property(x => x.JsonPathExpression).HasMaxLength(500);
        builder.Property(x => x.Type).HasConversion<string>();
        builder.Property(x => x.Operator).HasConversion<string>();
        builder.HasOne(x => x.Probe)
            .WithMany(x => x.Assertions)
            .HasForeignKey(x => x.ProbeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
