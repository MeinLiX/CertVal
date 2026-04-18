namespace CertVal.Core.Enums;

public enum OcspStatus
{
    /// <summary>The certificate has never been checked yet.</summary>
    NotChecked = 0,

    /// <summary>The certificate does not declare an OCSP responder URL (no AIA extension).</summary>
    NotConfigured = 1,

    /// <summary>An OCSP check was attempted but failed (network, parse, no issuer, etc.).</summary>
    CheckFailed = 2,

    /// <summary>OCSP responder confirmed the certificate is valid.</summary>
    Good = 3,

    /// <summary>OCSP responder reported the certificate as revoked.</summary>
    Revoked = 4
}
