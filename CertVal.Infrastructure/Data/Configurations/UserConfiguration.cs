using CertVal.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CertVal.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.EmailConfirmationToken)
            .HasMaxLength(255);

        builder.Property(u => u.PasswordResetToken)
            .HasMaxLength(255);

        builder.Property(u => u.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(u => u.TimeZone)
            .HasMaxLength(50)
            .HasDefaultValue("UTC");

        builder.Property(u => u.Language)
            .HasMaxLength(10)
            .HasDefaultValue("en");

        builder.Property(u => u.EmailNotificationsEnabled)
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");

        builder.HasIndex(u => u.EmailConfirmationToken)
            .HasDatabaseName("IX_Users_EmailConfirmationToken")
            .HasFilter("\"EmailConfirmationToken\" IS NOT NULL");

        builder.HasIndex(u => u.PasswordResetToken)
            .HasDatabaseName("IX_Users_PasswordResetToken")
            .HasFilter("\"PasswordResetToken\" IS NOT NULL");

        builder.HasIndex(u => u.Status)
            .HasDatabaseName("IX_Users_Status");

        // Relationships
        builder.HasMany(u => u.OwnedWorkspaces)
            .WithOne(w => w.Owner)
            .HasForeignKey(w => w.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.WorkspaceMemberships)
            .WithOne(wm => wm.User)
            .HasForeignKey(wm => wm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.ApiTokens)
            .WithOne(at => at.User)
            .HasForeignKey(at => at.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}