using CertVal.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CertVal.Infrastructure.Data.Configurations;

public class NotificationRuleConfiguration : IEntityTypeConfiguration<NotificationRule>
{
    public void Configure(EntityTypeBuilder<NotificationRule> builder)
    {
        builder.ToTable("NotificationRules");

        builder.HasKey(nr => nr.Id);

        builder.Property(nr => nr.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(nr => nr.DaysBeforeExpiry)
            .IsRequired();

        builder.Property(nr => nr.Frequency)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(nr => nr.ChannelType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(nr => nr.ChannelConfig)
            .IsRequired()
            .HasMaxLength(2000)
            .HasDefaultValue("{}");

        builder.Property(nr => nr.IsEnabled)
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(nr => nr.WorkspaceId)
            .HasDatabaseName("IX_NotificationRules_WorkspaceId");

        builder.HasIndex(nr => new { nr.WorkspaceId, nr.IsEnabled })
            .HasDatabaseName("IX_NotificationRules_Workspace_Enabled");

        // Relationships
        builder.HasOne(nr => nr.Workspace)
            .WithMany(w => w.NotificationRules)
            .HasForeignKey(nr => nr.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(nr => nr.NotificationHistory)
            .WithOne(nh => nh.NotificationRule)
            .HasForeignKey(nh => nh.NotificationRuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}