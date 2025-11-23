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

    public void SetPreviousCertificate(Guid previousCertificateId)
    {
        PreviousCertificateId = previousCertificateId;
        UpdatedAt = DateTime.UtcNow;
    }
}