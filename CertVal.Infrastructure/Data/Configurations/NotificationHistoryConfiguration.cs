using CertVal.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CertVal.Infrastructure.Data.Configurations;

public class NotificationHistoryConfiguration : IEntityTypeConfiguration<NotificationHistory>
{
    public void Configure(EntityTypeBuilder<NotificationHistory> builder)
    {
        builder.ToTable("NotificationHistory");

        builder.HasKey(nh => nh.Id);

        builder.Property(nh => nh.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(nh => nh.ChannelType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(nh => nh.Recipient)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(nh => nh.Subject)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(nh => nh.Message)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(nh => nh.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(nh => nh.ExternalId)
            .HasMaxLength(255);

        builder.Property(nh => nh.ResponseData)
            .HasColumnType("nvarchar(max)");

        builder.Property(nh => nh.RetryCount)
            .HasDefaultValue(0);

        builder.Property(nh => nh.MaxRetries)
            .HasDefaultValue(3);

        // Indexes
        builder.HasIndex(nh => nh.NotificationRuleId)
            .HasDatabaseName("IX_NotificationHistory_RuleId");

        builder.HasIndex(nh => nh.CertificateId)
            .HasDatabaseName("IX_NotificationHistory_CertificateId");

        builder.HasIndex(nh => new { nh.Status, nh.ScheduledAt })
            .HasDatabaseName("IX_NotificationHistory_Status_Scheduled");

        builder.HasIndex(nh => nh.ScheduledAt)
            .HasDatabaseName("IX_NotificationHistory_ScheduledAt");

        // Relationships
        builder.HasOne(nh => nh.NotificationRule)
            .WithMany(nr => nr.NotificationHistory)
            .HasForeignKey(nh => nh.NotificationRuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(nh => nh.Certificate)
            .WithMany(c => c.NotificationHistory)
            .HasForeignKey(nh => nh.CertificateId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}