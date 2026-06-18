using CertVal.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CertVal.Infrastructure.Data.Configurations;

public class MonitoredEndpointConfiguration : IEntityTypeConfiguration<MonitoredEndpoint>
{
    public void Configure(EntityTypeBuilder<MonitoredEndpoint> builder)
    {
        builder.ToTable("MonitoredEndpoints");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Host)
            .IsRequired()
            .HasMaxLength(253);

        builder.Property(e => e.Port)
            .IsRequired()
            .HasDefaultValue(443);

        builder.Property(e => e.IsEnabled)
            .HasDefaultValue(true);

        builder.Property(e => e.CheckIntervalMinutes)
            .HasDefaultValue(360);

        builder.Property(e => e.LastGrade).HasMaxLength(2);
        builder.Property(e => e.LastProtocol).HasMaxLength(32);
        builder.Property(e => e.LeafSubject).HasMaxLength(500);
        builder.Property(e => e.LeafThumbprint).HasMaxLength(64);
        builder.Property(e => e.LastError).HasMaxLength(500);

        builder.HasIndex(e => e.WorkspaceId)
            .HasDatabaseName("IX_MonitoredEndpoints_WorkspaceId");

        builder.HasIndex(e => new { e.WorkspaceId, e.Host, e.Port })
            .IsUnique()
            .HasDatabaseName("IX_MonitoredEndpoints_Workspace_Host_Port");

        builder.HasIndex(e => new { e.IsEnabled, e.LastCheckedAt })
            .HasDatabaseName("IX_MonitoredEndpoints_Due");

        builder.HasOne(e => e.Workspace)
            .WithMany()
            .HasForeignKey(e => e.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
