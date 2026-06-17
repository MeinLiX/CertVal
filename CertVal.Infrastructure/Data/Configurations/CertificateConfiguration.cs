using CertVal.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CertVal.Infrastructure.Data.Configurations;

public class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
{
    public void Configure(EntityTypeBuilder<Certificate> builder)
    {
        builder.ToTable("Certificates");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Subject)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.Issuer)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.SerialNumber)
            .HasMaxLength(100);

        builder.Property(c => c.Thumbprint)
            .IsRequired()
            .HasMaxLength(64); // SHA-256 hex string

        builder.Property(c => c.OriginalFileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(c => c.FilePath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.FileFormat)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(c => c.IsBundle)
            .HasDefaultValue(false);

        builder.Property(c => c.IsSkipped)
            .HasDefaultValue(false);

        builder.Property(c => c.Tags)
            .HasColumnType("text[]")
            .HasDefaultValueSql("'{}'::text[]");

        builder.Property(c => c.OcspStatus)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(CertVal.Core.Enums.OcspStatus.NotChecked);

        builder.Property(c => c.OcspResponderUrl)
            .HasMaxLength(500);

        builder.Property(c => c.OcspRevocationReason)
            .HasMaxLength(100);

        builder.Property(c => c.OcspLastError)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(c => c.WorkspaceId)
            .HasDatabaseName("IX_Certificates_WorkspaceId");

        builder.HasIndex(c => c.Thumbprint)
            .HasDatabaseName("IX_Certificates_Thumbprint");

        builder.HasIndex(c => c.NotAfter)
            .HasDatabaseName("IX_Certificates_NotAfter");

        builder.HasIndex(c => new { c.WorkspaceId, c.NotAfter })
            .HasDatabaseName("IX_Certificates_Workspace_Expiry");

        builder.HasIndex(c => c.ParentCertificateId)
            .HasDatabaseName("IX_Certificates_ParentId")
            .HasFilter("\"ParentCertificateId\" IS NOT NULL"); ;

        builder.HasIndex(c => new { c.WorkspaceId, c.Status })
            .HasDatabaseName("IX_Certificates_Workspace_Status");

        builder.HasIndex(c => new { c.WorkspaceId, c.IsBundle })
            .HasDatabaseName("IX_Certificates_Workspace_Bundle");

        builder.HasIndex(c => new { c.WorkspaceId, c.Issuer, c.SerialNumber })
            .IsUnique()
            .HasFilter("\"SerialNumber\" IS NOT NULL AND \"IsBundle\" = false")
            .HasDatabaseName("IX_Certificates_Workspace_Issuer_Serial");

        builder.HasIndex(c => new { c.OcspLastCheckedAt, c.OcspStatus })
            .HasDatabaseName("IX_Certificates_OcspLastCheckedAt");

        // Relationships
        builder.HasOne(c => c.Workspace)
            .WithMany(w => w.Certificates)
            .HasForeignKey(c => c.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.ParentCertificate)
            .WithMany(c => c.ChildCertificates)
            .HasForeignKey(c => c.ParentCertificateId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.PreviousCertificate)
            .WithMany()
            .HasForeignKey(c => c.PreviousCertificateId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(c => c.NotificationHistory)
            .WithOne(nh => nh.Certificate)
            .HasForeignKey(nh => nh.CertificateId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}