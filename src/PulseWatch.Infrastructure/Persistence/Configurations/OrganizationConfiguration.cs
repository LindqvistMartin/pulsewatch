using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PulseWatch.Core.Entities;

namespace PulseWatch.Infrastructure.Persistence.Configurations;

internal sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Slug).IsUnique();
    }
}
