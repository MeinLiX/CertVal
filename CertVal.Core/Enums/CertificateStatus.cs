namespace CertVal.Core.Enums;

public enum CertificateStatus
{
    Active = 1,
    Expired = 2,
    Revoked = 3,
    Invalid = 4
}

public enum CertificateStatusFilter
{
    All = 0,

    Valid = 1,

    /// <summary>
    /// Certificates expiring within 30 days but not yet expired
    /// </summary>
    Expiring = 2,

    Expired = 3
}
