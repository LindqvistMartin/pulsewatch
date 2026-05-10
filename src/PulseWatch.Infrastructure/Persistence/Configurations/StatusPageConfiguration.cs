using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PulseWatch.Core.Entities;

namespace PulseWatch.Infrastructure.Persistence.Configurations;

internal sealed class StatusPageConfiguration : IEntityTypeConfiguration<StatusPage>
{
    public void Configure(EntityTypeBuilder<StatusPage> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Slug).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.ProbeIds)
            .HasColumnType("jsonb")
            .IsRequired()
            .HasConversion(
                new ValueConverter<List<Guid>, string>(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>()
                ),
                new ValueComparer<List<Guid>>(
                    (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
                    v => v == null ? 0 : v.Aggregate(0, (h, g) => HashCode.Combine(h, g.GetHashCode())),
                    v => v == null ? new List<Guid>() : v.ToList()
                )
            );
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasOne(x => x.Project)
            .WithMany()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
