using CertVal.Application.Common.Interfaces;
using CertVal.Core.Enums;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Ocsp;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;
using System.Net.Http.Headers;

namespace CertVal.Infrastructure.Services;

/// <summary>
/// BouncyCastle-backed OCSP responder client.
/// </summary>
public sealed class OcspCheckService : IOcspCheckService
{
    private static readonly DerObjectIdentifier OcspAccessMethod = AccessDescription.IdADOcsp;
    private static readonly DerObjectIdentifier CaIssuersAccessMethod = AccessDescription.IdADCAIssuers;

    private static readonly MediaTypeHeaderValue OcspRequestContentType = MediaTypeHeaderValue.Parse("application/ocsp-request");

    private readonly HttpClient _httpClient;
    private readonly ILogger<OcspCheckService> _logger;

    public OcspCheckService(HttpClient httpClient, ILogger<OcspCheckService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<OcspCheckResult> CheckAsync(byte[] certificateBytes, string originalFileName, CancellationToken cancellationToken = default)
    {
        if (certificateBytes is null || certificateBytes.Length == 0)
            return new OcspCheckResult(OcspStatus.CheckFailed, null, Error: "Empty certificate payload");

        X509Certificate? leaf;
        try
        {
            leaf = ParseLeaf(certificateBytes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse certificate {FileName}", originalFileName);
            return new OcspCheckResult(OcspStatus.CheckFailed, null, Error: "Unable to parse certificate");
        }

        if (leaf is null)
            return new OcspCheckResult(OcspStatus.CheckFailed, null, Error: "Unable to parse certificate");

        var (ocspUrl, caIssuersUrl) = ExtractAiaUrls(leaf);
        if (string.IsNullOrWhiteSpace(ocspUrl))
            return new OcspCheckResult(OcspStatus.NotConfigured, null);

        X509Certificate? issuer;
        try
        {
            issuer = await ResolveIssuerAsync(leaf, caIssuersUrl, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download issuer certificate for {Subject}", leaf.SubjectDN);
            return new OcspCheckResult(OcspStatus.CheckFailed, ocspUrl, Error: "Issuer certificate unavailable");
        }

        if (issuer is null)
            return new OcspCheckResult(OcspStatus.CheckFailed, ocspUrl, Error: "Issuer certificate not found");

        try
        {
            var requestBytes = BuildOcspRequest(leaf, issuer);
            var response = await PostOcspRequestAsync(ocspUrl, requestBytes, cancellationToken);
            return InterpretResponse(response, ocspUrl);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OCSP check failed for {Subject} via {Url}", leaf.SubjectDN, ocspUrl);
            return new OcspCheckResult(OcspStatus.CheckFailed, ocspUrl, Error: ex.Message);
        }
    }

    private static X509Certificate? ParseLeaf(byte[] bytes)
    {
        var parser = new X509CertificateParser();
        // X509CertificateParser auto-detects PEM vs DER.
        return parser.ReadCertificate(bytes);
    }

    private static (string? OcspUrl, string? CaIssuersUrl) ExtractAiaUrls(X509Certificate cert)
    {
        var encoded = cert.GetExtensionValue(X509Extensions.AuthorityInfoAccess);
        if (encoded is null)
            return (null, null);

        var asn1 = X509ExtensionUtilities.FromExtensionValue(encoded);
        var aia = AuthorityInformationAccess.GetInstance(asn1);

        string? ocspUrl = null;
        string? caUrl = null;

        foreach (var description in aia.GetAccessDescriptions())
        {
            var location = description.AccessLocation;
            if (location.TagNo != GeneralName.UniformResourceIdentifier) continue;

            var url = ExtractUriString(location);
            if (string.IsNullOrWhiteSpace(url)) continue;

            if (description.AccessMethod.Equals(OcspAccessMethod) && ocspUrl is null)
                ocspUrl = url;
            else if (description.AccessMethod.Equals(CaIssuersAccessMethod) && caUrl is null)
                caUrl = url;
        }

        return (ocspUrl, caUrl);
    }

    private static string? ExtractUriString(GeneralName name)
    {
        return name.Name switch
        {
            DerIA5String ia5 => ia5.GetString(),
            IAsn1String s => s.GetString(),
            _ => name.Name?.ToString()
        };
    }

    private async Task<X509Certificate?> ResolveIssuerAsync(X509Certificate leaf, string? caIssuersUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(caIssuersUrl))
            return null;

        if (!Uri.TryCreate(caIssuersUrl, UriKind.Absolute, out var uri))
            return null;

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return null;

        using var response = await _httpClient.GetAsync(uri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogDebug("CA issuers download returned {Status} from {Url}", response.StatusCode, uri);
            return null;
        }

        var contentBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (contentBytes.Length == 0)
            return null;

        // Some responders return PKCS#7 bundles; X509CertificateParser handles both single and bundled formats.
        var parser = new X509CertificateParser();
        try
        {
            var certs = parser.ReadCertificates(contentBytes);
            foreach (X509Certificate candidate in certs)
            {
                if (candidate.SubjectDN.Equivalent(leaf.IssuerDN))
                    return candidate;
            }

            // Fallback: single certificate stream.
            return parser.ReadCertificate(contentBytes);
        }
        catch
        {
            return null;
        }
    }

    private static byte[] BuildOcspRequest(X509Certificate leaf, X509Certificate issuer)
    {
        var generator = new OcspReqGenerator();
        var certificateId = new CertificateID(CertificateID.HashSha1, issuer, leaf.SerialNumber);
        generator.AddRequest(certificateId);
        var request = generator.Generate();
        return request.GetEncoded();
    }

    private async Task<OcspResp> PostOcspRequestAsync(string responderUrl, byte[] requestBytes, CancellationToken cancellationToken)
    {
        using var content = new ByteArrayContent(requestBytes);
        content.Headers.ContentType = OcspRequestContentType;

        using var response = await _httpClient.PostAsync(responderUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        return new OcspResp(responseBytes);
    }

    private static OcspCheckResult InterpretResponse(OcspResp response, string responderUrl)
    {
        if (response.Status != OcspRespStatus.Successful)
        {
            return new OcspCheckResult(
                OcspStatus.CheckFailed,
                responderUrl,
                Error: $"OCSP responder status: {response.Status}");
        }

        if (response.GetResponseObject() is not BasicOcspResp basic)
        {
            return new OcspCheckResult(
                OcspStatus.CheckFailed,
                responderUrl,
                Error: "OCSP response missing basic response payload");
        }

        var single = basic.Responses?.FirstOrDefault();
        if (single is null)
        {
            return new OcspCheckResult(
                OcspStatus.CheckFailed,
                responderUrl,
                Error: "OCSP response did not contain a status entry");
        }

        var status = single.GetCertStatus();

        // BouncyCastle convention: CertificateStatus.Good is represented by null.
        if (status is null)
            return new OcspCheckResult(OcspStatus.Good, responderUrl);

        if (status is RevokedStatus revoked)
        {
            string? reason = null;
            if (revoked.HasRevocationReason)
            {
                reason = MapRevocationReason(revoked.RevocationReason);
            }

            DateTime? revokedAt = null;
            try
            {
                revokedAt = DateTime.SpecifyKind(revoked.RevocationTime, DateTimeKind.Utc);
            }
            catch
            {
                // ignore parse issues – status is still Revoked
            }

            return new OcspCheckResult(OcspStatus.Revoked, responderUrl, reason, revokedAt);
        }

        // UnknownStatus or anything else
        return new OcspCheckResult(OcspStatus.CheckFailed, responderUrl, Error: "OCSP responder returned Unknown status");
    }

    private static string MapRevocationReason(int reasonCode) => reasonCode switch
    {
        0 => "Unspecified",
        1 => "KeyCompromise",
        2 => "CaCompromise",
        3 => "AffiliationChanged",
        4 => "Superseded",
        5 => "CessationOfOperation",
        6 => "CertificateHold",
        8 => "RemoveFromCrl",
        9 => "PrivilegeWithdrawn",
        10 => "AaCompromise",
        _ => $"Reason{reasonCode}"
    };
}
