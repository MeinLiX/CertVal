using CertVal.Core.Enums;
using CertVal.Core.Events;

namespace CertVal.Core.Entities;

public class Certificate : BaseEntity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid WorkspaceId { get; private set; }

    // Certificate metadata
    public string Subject { get; private set; } = string.Empty;
    public string Issuer { get; private set; } = string.Empty;
    public string? SerialNumber { get; private set; }
    public string Thumbprint { get; private set; } = string.Empty;

    // Validity
    public DateTime NotBefore { get; private set; }
    public DateTime NotAfter { get; private set; }

    // File info
    public string OriginalFileName { get; private set; } = string.Empty;
    public string FilePath { get; private set; } = string.Empty;
    public CertificateFormat FileFormat { get; private set; }
    public long FileSize { get; private set; }

    // Bundle support
    public Guid? ParentCertificateId { get; private set; }
    public bool IsBundle { get; private set; }

    // Status
    public CertificateStatus Status { get; private set; } = CertificateStatus.Active;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    // Monitoring
    public bool IsSkipped { get; private set; }

    // Organization
    public List<string> Tags { get; private set; } = [];

    // OCSP revocation tracking
    public OcspStatus OcspStatus { get; private set; } = OcspStatus.NotChecked;
    public DateTime? OcspLastCheckedAt { get; private set; }
    public string? OcspResponderUrl { get; private set; }
    public string? OcspRevocationReason { get; private set; }
    public DateTime? OcspRevokedAt { get; private set; }
    public string? OcspLastError { get; private set; }

    // Versioning
    public Guid? PreviousCertificateId { get; private set; }
    public virtual Certificate? PreviousCertificate { get; private set; }

    // Navigation properties
    public virtual Workspace Workspace { get; private set; } = null!;
    public virtual Certificate? ParentCertificate { get; private set; }
    public virtual ICollection<Certificate> ChildCertificates { get; private set; } = [];
    public virtual ICollection<NotificationHistory> NotificationHistory { get; private set; } = [];

    private Certificate() { } // EF Constructor

    public static Certificate Create(
        Guid workspaceId,
        string subject,
        string issuer,
        string thumbprint,
        DateTime notBefore,
        DateTime notAfter,
        string originalFileName,
        string filePath,
        CertificateFormat fileFormat,
        long fileSize,
        string? serialNumber = null,
        Guid? parentCertificateId = null,
        bool isBundle = false)
    {
        var certificate = new Certificate
        {
            WorkspaceId = workspaceId,
            Subject = subject,
            Issuer = issuer,
            SerialNumber = serialNumber,
            Thumbprint = thumbprint,
            NotBefore = notBefore,
            NotAfter = notAfter,
            OriginalFileName = originalFileName,
            FilePath = filePath,
            FileFormat = fileFormat,
            FileSize = fileSize,
            ParentCertificateId = parentCertificateId,
            IsBundle = isBundle
        };

        certificate.AddDomainEvent(new CertificateUploadedEvent(
            certificate.Id,
            certificate.WorkspaceId,
            certificate.Subject,
            certificate.NotAfter));

        return certificate;
    }

    public static void CreateBundle(Guid parentCertificateId, Guid workspaceId, List<Certificate> childCertificates)
    {
        if (childCertificates.Count > 0)
        {
            var bundleEvent = new CertificateBundleProcessedEvent(parentCertificateId, workspaceId, childCertificates.Count);

            // Add event to the first child certificate to trigger processing
            childCertificates[0].AddDomainEvent(bundleEvent);
        }
    }

    public void CheckExpiry()
    {
        var daysUntilExpiry = (NotAfter - DateTime.UtcNow).Days;

        if (IsExpired && Status != CertificateStatus.Expired)
        {
            AddDomainEvent(new CertificateExpiredEvent(Id, WorkspaceId, Subject, NotAfter));
        }
        else if (daysUntilExpiry <= 30 && daysUntilExpiry > 0)
        {
            AddDomainEvent(new CertificateExpiringEvent(Id, WorkspaceId, Subject, NotAfter, daysUntilExpiry));
        }
    }

    public void UpdateFilePath(string filePath)
    {
        FilePath = filePath;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsExpired => DateTime.UtcNow > NotAfter;
    public bool IsExpiringSoon(int days) => DateTime.UtcNow.AddDays(days) >= NotAfter && !IsExpired;

    public void MarkAsExpired()
    {
        Status = CertificateStatus.Expired;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new CertificateExpiredEvent(Id, WorkspaceId, Subject, NotAfter));
    }

    public void ToggleSkipMonitoring(bool isSkipped)
    {
        IsSkipped = isSkipped;
        UpdatedAt = DateTime.UtcNow;
    }

    public const int MaxTags = 25;
    public const int MaxTagLength = 40;

    /// <summary>
    /// Replaces the certificate's tags with a normalized set: trimmed, de-duplicated
    /// case-insensitively, empties removed, each capped at <see cref="MaxTagLength"/>
    /// characters and the whole set capped at <see cref="MaxTags"/> entries.
    /// </summary>
    public void SetTags(IEnumerable<string>? tags)
    {
        var normalized = new List<string>();
        if (tags is not null)
        {
            foreach (var raw in tags)
            {
                var tag = raw?.Trim();
                if (string.IsNullOrEmpty(tag)) continue;
                if (tag.Length > MaxTagLength) tag = tag[..MaxTagLength];
                if (!normalized.Any(x => string.Equals(x, tag, StringComparison.OrdinalIgnoreCase)))
                    normalized.Add(tag);
                if (normalized.Count >= MaxTags) break;
            }
        }

        Tags = normalized;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPreviousCertificate(Guid previousCertificateId)
    {
        PreviousCertificateId = previousCertificateId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the OCSP revocation state. Raises a <see cref="CertificateRevokedEvent"/>
    /// the first time the certificate transitions into <see cref="OcspStatus.Revoked"/>.
    /// </summary>
    public void UpdateOcspStatus(
        OcspStatus newStatus,
        string? responderUrl = null,
        string? revocationReason = null,
        DateTime? revokedAt = null,
        string? lastError = null)
    {
        var transitioningToRevoked = newStatus == OcspStatus.Revoked && OcspStatus != OcspStatus.Revoked;

        OcspStatus = newStatus;
        OcspLastCheckedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(responderUrl))
            OcspResponderUrl = responderUrl;

        OcspRevocationReason = newStatus == OcspStatus.Revoked ? revocationReason : null;
        OcspRevokedAt = newStatus == OcspStatus.Revoked ? (revokedAt ?? OcspRevokedAt ?? DateTime.UtcNow) : null;
        OcspLastError = newStatus == OcspStatus.CheckFailed ? lastError : null;

        if (newStatus == OcspStatus.Revoked && Status != CertificateStatus.Revoked)
            Status = CertificateStatus.Revoked;

        UpdatedAt = DateTime.UtcNow;

        if (transitioningToRevoked)
        {
            AddDomainEvent(new CertificateRevokedEvent(
                Id,
                WorkspaceId,
                Subject,
                Issuer,
                OcspRevokedAt ?? DateTime.UtcNow,
                revocationReason));
        }
    }
}