using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PulseWatch.Core.Entities;

namespace PulseWatch.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();
        // Partial index: only unprocessed rows are indexed. Avoids full-table scan as the
        // append-only table grows — the relay's WHERE "ProcessedAt" IS NULL filter uses this index.
        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("IX_OutboxMessages_Unprocessed")
            .HasFilter("""("ProcessedAt" IS NULL)""");
    }
}
