using CertVal.Application.Common.Models;
using CertVal.Core.Enums;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;

namespace CertVal.Application.Services;

public class CertificateProcessorService : ICertificateProcessorService
{
    private readonly ILogger<CertificateProcessorService> _logger;

    public CertificateProcessorService(ILogger<CertificateProcessorService> logger)
    {
        _logger = logger;
    }

    public async Task<Result<IEnumerable<ParsedCertificateInfo>>> ProcessCertificateAsync(
        byte[] certificateData,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var format = DetermineFormat(extension);

            return await Task.Run(() => ProcessByFormat(certificateData, format), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process certificate {FileName}", fileName);
            return Result.Failure<IEnumerable<ParsedCertificateInfo>>($"Failed to process certificate: {ex.Message}");
        }
    }

    private Result<IEnumerable<ParsedCertificateInfo>> ProcessByFormat(byte[] data, CertificateFormat format)
    {
        var certificates = new List<ParsedCertificateInfo>();

        try
        {
            switch (format)
            {
                case CertificateFormat.P7B:
                case CertificateFormat.P7C:
                    certificates.AddRange(ProcessPkcs7Bundle(data));
                    break;

                case CertificateFormat.PFX:
                case CertificateFormat.P12:
                    certificates.AddRange(ProcessPfxBundle(data));
                    break;

                default:
                    var cert = ProcessSingleCertificate(data, format);
                    if (cert != null)
                        certificates.Add(cert);
                    break;
            }

            if (!certificates.Any())
                return Result.Failure<IEnumerable<ParsedCertificateInfo>>("No valid certificates found");

            return Result.Success(certificates.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Failure<IEnumerable<ParsedCertificateInfo>>($"Certificate processing failed: {ex.Message}");
        }
    }

    private IEnumerable<ParsedCertificateInfo> ProcessPkcs7Bundle(byte[] data)
    {
        var collection = new X509Certificate2Collection();
        collection.Import(data);

        return collection.Cast<X509Certificate2>()
            .Select(cert => CreateParsedInfo(cert, CertificateFormat.P7B))
            .ToList();
    }

    private IEnumerable<ParsedCertificateInfo> ProcessPfxBundle(byte[] data)
    {
        // For PFX, we might need a password - try empty password
        try
        {
            var collection = new X509Certificate2Collection();
            collection.Import(data, "", X509KeyStorageFlags.Exportable);

            return collection.Cast<X509Certificate2>()
                .Select(cert => CreateParsedInfo(cert, CertificateFormat.PFX))
                .ToList();
        }
        catch
        {
            throw new InvalidOperationException("PFX file is password protected. Please provide password.");
        }
    }

    private ParsedCertificateInfo? ProcessSingleCertificate(byte[] data, CertificateFormat format)
    {
        try
        {
            var cert = new X509Certificate2(data);
            return CreateParsedInfo(cert, format);
        }
        catch
        {
            // Try alternative import methods
            try
            {
                var cert = X509Certificate2.CreateFromPem(System.Text.Encoding.UTF8.GetString(data));
                return CreateParsedInfo(cert, format);
            }
            catch
            {
                return null;
            }
        }
    }

    private ParsedCertificateInfo CreateParsedInfo(X509Certificate2 cert, CertificateFormat format)
    {
        return new ParsedCertificateInfo
        {
            Subject = cert.Subject,
            Issuer = cert.Issuer,
            SerialNumber = cert.SerialNumber,
            Thumbprint = cert.Thumbprint,
            NotBefore = cert.NotBefore.ToUniversalTime(),
            NotAfter = cert.NotAfter.ToUniversalTime(),
            Format = format
        };
    }

    private CertificateFormat DetermineFormat(string extension)
    {
        return extension switch
        {
            ".cer" => CertificateFormat.CER,
            ".crt" => CertificateFormat.CRT,
            ".pem" => CertificateFormat.PEM,
            ".der" => CertificateFormat.DER,
            ".p7b" => CertificateFormat.P7B,
            ".p7c" => CertificateFormat.P7C,
            ".pfx" => CertificateFormat.PFX,
            ".p12" => CertificateFormat.P12,
            _ => CertificateFormat.Unknown
        };
    }
}