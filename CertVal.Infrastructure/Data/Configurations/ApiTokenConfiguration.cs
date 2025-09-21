using CertVal.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CertVal.Infrastructure.Data.Configurations;

public class ApiTokenConfiguration : IEntityTypeConfiguration<ApiToken>
{
    public void Configure(EntityTypeBuilder<ApiToken> builder)
    {
        builder.ToTable("ApiTokens");

        builder.HasKey(at => at.Id);

        builder.Property(at => at.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(at => at.TokenHash)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(at => at.TokenPrefix)
            .IsRequired()
            .HasMaxLength(16);

        builder.Property(at => at.Scope)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(at => at.IsActive)
            .HasDefaultValue(true);

        builder.Property(at => at.LastUsedIpAddress)
            .HasMaxLength(45); // IPv6 address length

        // Indexes
        builder.HasIndex(at => at.UserId)
            .HasDatabaseName("IX_ApiTokens_UserId");

        builder.HasIndex(at => at.TokenHash)
            .IsUnique()
            .HasDatabaseName("IX_ApiTokens_TokenHash");

        builder.HasIndex(at => new { at.UserId, at.IsActive })
            .HasDatabaseName("IX_ApiTokens_User_Active");

        builder.HasIndex(at => at.ExpiresAt)
            .HasDatabaseName("IX_ApiTokens_ExpiresAt")
            .HasFilter("\"ExpiresAt\" IS NOT NULL");

        // Relationships
        builder.HasOne(at => at.User)
            .WithMany(u => u.ApiTokens)
            .HasForeignKey(at => at.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}