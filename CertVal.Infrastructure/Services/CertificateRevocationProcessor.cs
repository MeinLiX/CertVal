using CertVal.Application.Common.Interfaces;
using CertVal.Core.Enums;
using CertVal.Core.Repositories;
using CertVal.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CertVal.Infrastructure.Services;


public sealed class CertificateRevocationProcessor : ICertificateRevocationProcessor
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICertificateStorageService _storageService;
    private readonly IOcspCheckService _ocspCheckService;
    private readonly OcspCheckingConfiguration _options;
    private readonly ILogger<CertificateRevocationProcessor> _logger;

    public CertificateRevocationProcessor(
        IUnitOfWork unitOfWork,
        ICertificateStorageService storageService,
        IOcspCheckService ocspCheckService,
        IOptions<OcspCheckingConfiguration> options,
        ILogger<CertificateRevocationProcessor> logger)
    {
        _unitOfWork = unitOfWork;
        _storageService = storageService;
        _ocspCheckService = ocspCheckService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ProcessRevocationChecksAsync(CancellationToken cancellationToken = default)
    {
        var batchSize = _options.BatchSize;
        var minInterval = TimeSpan.FromMinutes(_options.MinPerCertificateIntervalMinutes);
        var maxConcurrency = Math.Max(1, _options.MaxConcurrency);

        var due = await _unitOfWork.Certificates.GetForOcspCheckAsync(batchSize, minInterval, cancellationToken);
        if (due.Count == 0)
        {
            _logger.LogDebug("OCSP cycle: no certificates are due for revocation check");
            return;
        }

        _logger.LogInformation(
            "OCSP cycle: checking {Count} certificate(s) (batch size {Batch}, concurrency {Concurrency})",
            due.Count, batchSize, maxConcurrency);

        var queue = due.Select(c => (c.Id, c.FilePath, c.OriginalFileName)).ToList();
        var results = new System.Collections.Concurrent.ConcurrentDictionary<Guid, OcspCheckResult>();

        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = maxConcurrency
        };

        await Parallel.ForEachAsync(queue, parallelOptions, async (item, ct) =>
        {
            var result = await CheckSingleAsync(item.Id, item.FilePath, item.OriginalFileName, ct);
            results[item.Id] = result;
        });

        foreach (var (certId, result) in results)
        {
            try
            {
                var entity = due.FirstOrDefault(c => c.Id == certId)
                    ?? await _unitOfWork.Certificates.GetByIdAsync(certId, cancellationToken);
                if (entity is null) continue;

                entity.UpdateOcspStatus(
                    result.Status,
                    result.ResponderUrl,
                    result.RevocationReason,
                    result.RevokedAt,
                    result.Error);

                await _unitOfWork.Certificates.UpdateAsync(entity, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply OCSP status update for certificate {CertificateId}", certId);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var revoked = results.Count(r => r.Value.Status == OcspStatus.Revoked);
        var good = results.Count(r => r.Value.Status == OcspStatus.Good);
        var notConfigured = results.Count(r => r.Value.Status == OcspStatus.NotConfigured);
        var failed = results.Count(r => r.Value.Status == OcspStatus.CheckFailed);

        _logger.LogInformation(
            "OCSP cycle complete: good={Good}, revoked={Revoked}, notConfigured={NotConfigured}, failed={Failed}",
            good, revoked, notConfigured, failed);
    }

    private async Task<OcspCheckResult> CheckSingleAsync(Guid certificateId, string filePath, string originalFileName, CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await _storageService.GetCertificateAsync(filePath, cancellationToken);
            return await _ocspCheckService.CheckAsync(bytes, originalFileName, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OCSP check threw for certificate {CertificateId}", certificateId);
            return new OcspCheckResult(OcspStatus.CheckFailed, null, Error: ex.Message);
        }
    }
}
