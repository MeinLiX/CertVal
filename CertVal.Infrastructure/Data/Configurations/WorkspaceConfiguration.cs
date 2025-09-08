using CertVal.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CertVal.Infrastructure.Data.Configurations;

public class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.ToTable("Workspaces");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(w => w.Description)
            .HasMaxLength(1000);

        builder.Property(w => w.MaxCertificates)
            .HasDefaultValue(1000);

        builder.Property(w => w.IsPublic)
            .HasDefaultValue(false);

        builder.Property(w => w.AllowMemberInvites)
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(w => w.OwnerId)
            .HasDatabaseName("IX_Workspaces_OwnerId");

        builder.HasIndex(w => new { w.OwnerId, w.Name })
            .IsUnique()
            .HasDatabaseName("IX_Workspaces_Owner_Name");

        // Relationships
        builder.HasOne(w => w.Owner)
            .WithMany(u => u.OwnedWorkspaces)
            .HasForeignKey(w => w.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(w => w.Certificates)
            .WithOne(c => c.Workspace)
            .HasForeignKey(c => c.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(w => w.NotificationRules)
            .WithOne(nr => nr.Workspace)
            .HasForeignKey(nr => nr.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(w => w.Members)
            .WithOne(wm => wm.Workspace)
            .HasForeignKey(wm => wm.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}