using CertVal.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CertVal.Infrastructure.Data.Configurations;

public class StoredEventConfiguration : IEntityTypeConfiguration<StoredEvent>
{
    public void Configure(EntityTypeBuilder<StoredEvent> builder)
    {
        builder.ToTable("StoredEvents");

        builder.HasKey(se => se.Id);
        builder.Property(se => se.Id)
            .ValueGeneratedOnAdd()
            .UseIdentityColumn(1, 1);

        builder.Property(se => se.EventId)
            .IsRequired();

        builder.Property(se => se.EventType)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(se => se.EventData)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(se => se.AggregateType)
            .HasMaxLength(100);

        builder.Property(se => se.AggregateId);

        builder.Property(se => se.UserId)
            .HasMaxLength(450);

        builder.Property(se => se.CorrelationId)
            .HasMaxLength(100);

        builder.Property(se => se.OccurredAt)
            .IsRequired();

        builder.Property(se => se.StoredAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(se => se.Metadata)
            .HasColumnType("nvarchar(max)");

        // Indexes for common queries
        builder.HasIndex(se => se.Id)
            .HasDatabaseName("IX_StoredEvents_Id")
            .IsUnique();

        builder.HasIndex(se => se.EventId)
            .IsUnique()
            .HasDatabaseName("IX_StoredEvents_EventId");

        builder.HasIndex(se => se.EventType)
            .HasDatabaseName("IX_StoredEvents_EventType");

        builder.HasIndex(se => new { se.AggregateType, se.AggregateId })
            .HasDatabaseName("IX_StoredEvents_Aggregate");

        builder.HasIndex(se => se.UserId)
            .HasDatabaseName("IX_StoredEvents_UserId")
            .HasFilter("[UserId] IS NOT NULL");

        builder.HasIndex(se => se.CorrelationId)
            .HasDatabaseName("IX_StoredEvents_CorrelationId")
            .HasFilter("[CorrelationId] IS NOT NULL");

        builder.HasIndex(se => se.OccurredAt)
            .HasDatabaseName("IX_StoredEvents_OccurredAt");

        builder.HasIndex(se => se.StoredAt)
            .HasDatabaseName("IX_StoredEvents_StoredAt");

        builder.HasIndex(se => new { se.EventType, se.OccurredAt })
            .HasDatabaseName("IX_StoredEvents_Type_OccurredAt");

        builder.HasIndex(se => new { se.AggregateType, se.AggregateId, se.OccurredAt })
            .HasDatabaseName("IX_StoredEvents_Aggregate_OccurredAt");

        builder.HasIndex(se => new { se.UserId, se.OccurredAt })
            .HasDatabaseName("IX_StoredEvents_User_OccurredAt")
            .HasFilter("[UserId] IS NOT NULL");
    }
}