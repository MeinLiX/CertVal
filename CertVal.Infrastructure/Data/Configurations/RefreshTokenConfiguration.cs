using CertVal.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CertVal.Infrastructure.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.TokenHash)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(rt => rt.ReplacedByTokenHash)
            .HasMaxLength(255);

        builder.Property(rt => rt.ReasonRevoked)
            .HasMaxLength(200);

        builder.Property(rt => rt.CreatedByIp)
            .HasMaxLength(45); // IPv6 address length

        builder.Property(rt => rt.RevokedByIp)
            .HasMaxLength(45);

        // Indexes
        builder.HasIndex(rt => rt.TokenHash)
            .IsUnique()
            .HasDatabaseName("IX_RefreshTokens_TokenHash");

        builder.HasIndex(rt => rt.UserId)
            .HasDatabaseName("IX_RefreshTokens_UserId");

        builder.HasIndex(rt => rt.ExpiresAt)
            .HasDatabaseName("IX_RefreshTokens_ExpiresAt");

        // Relationships
        builder.HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
