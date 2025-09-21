using CertVal.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CertVal.Infrastructure.Data.Configurations;

public class WorkspaceMemberConfiguration : IEntityTypeConfiguration<WorkspaceMember>
{
    public void Configure(EntityTypeBuilder<WorkspaceMember> builder)
    {
        builder.ToTable("WorkspaceMembers");

        builder.HasKey(wm => wm.Id);

        builder.Property(wm => wm.Role)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(wm => wm.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(wm => wm.InvitationToken)
            .HasMaxLength(255);

        // Indexes
        builder.HasIndex(wm => wm.InvitationToken)
            .HasDatabaseName("IX_WorkspaceMembers_InvitationToken")
            .HasFilter("[InvitationToken] IS NOT NULL");

        builder.HasIndex(wm => wm.WorkspaceId)
            .HasDatabaseName("IX_WorkspaceMembers_WorkspaceId");

        builder.HasIndex(wm => wm.UserId)
            .HasDatabaseName("IX_WorkspaceMembers_UserId");

        builder.HasIndex(wm => new { wm.WorkspaceId, wm.UserId })
            .IsUnique()
            .HasDatabaseName("IX_WorkspaceMembers_Workspace_User");

        builder.HasIndex(wm => wm.InvitedByUserId)
            .HasDatabaseName("IX_WorkspaceMembers_InvitedBy")
            .HasFilter("[InvitedByUserId] IS NOT NULL");

        // Relationships
        builder.HasOne(wm => wm.Workspace)
            .WithMany(w => w.Members)
            .HasForeignKey(wm => wm.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(wm => wm.User)
            .WithMany(u => u.WorkspaceMemberships)
            .HasForeignKey(wm => wm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(wm => wm.InvitedByUser)
            .WithMany()
            .HasForeignKey(wm => wm.InvitedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}