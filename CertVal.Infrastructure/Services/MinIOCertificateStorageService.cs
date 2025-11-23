using CertVal.Application.Common.Interfaces;
using CertVal.Application.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using System.Text;

namespace CertVal.Infrastructure.Services;

public class MinIOCertificateStorageService : ICertificateStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly ILogger<MinIOCertificateStorageService> _logger;
    private readonly CertificateStorageConfiguration _config;

    public MinIOCertificateStorageService(
        IMinioClient minioClient,
        ILogger<MinIOCertificateStorageService> logger,
        IOptions<CertificateStorageConfiguration> config)
    {
        _minioClient = minioClient;
        _logger = logger;
        _config = config.Value;
    }

    public async Task<string> StoreCertificateAsync(Guid workspaceId, string fileName, byte[] fileContent, CancellationToken cancellationToken = default)
    {
        var certificateId = Guid.NewGuid();
        return await StoreCertificateAsync(workspaceId, certificateId, fileName, fileContent, cancellationToken);
    }

    public async Task<string> StoreCertificateAsync(Guid workspaceId, Guid certificateId, string fileName, byte[] fileContent, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureBucketExistsAsync(cancellationToken);

            var fileExtension = Path.GetExtension(fileName);
            var objectKey = _config.GetObjectKey(workspaceId, $"{certificateId:N}{fileExtension}");

            _logger.LogDebug("Storing certificate '{OriginalFileName}' with ObjectKey: '{ObjectKey}'", fileName, objectKey);

            using var stream = new MemoryStream(fileContent);

            var meta = new Dictionary<string, string>
            {
                ["x-amz-meta-workspace-id"] = workspaceId.ToString(),
                ["x-amz-meta-certificate-id"] = certificateId.ToString(),
                ["x-amz-meta-upload-date"] = DateTimeOffset.UtcNow.ToString("O"),
                ["x-amz-meta-original-filename-encoding"] = "utf-8-base64",
                ["x-amz-meta-original-filename"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(fileName))
            };

            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_config.BucketName)
                .WithObject(objectKey)
                .WithStreamData(stream)
                .WithObjectSize(fileContent.Length)
                .WithContentType("application/octet-stream")
                .WithHeaders(meta);

            await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);

            return objectKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store certificate {FileName} for workspace {WorkspaceId}", fileName, workspaceId);
            throw new InvalidOperationException($"Failed to store certificate {fileName}: {ex.Message}", ex);
        }
    }

    public async Task<byte[]> GetCertificateAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting certificate with ObjectKey: '{ObjectKey}' from bucket '{Bucket}'",
                objectKey, _config.BucketName);

            using var stream = new MemoryStream();

            var getObjectArgs = new GetObjectArgs()
                .WithBucket(_config.BucketName)
                .WithObject(objectKey)
                .WithCallbackStream(async (inputStream, ct) =>
                {
                    await inputStream.CopyToAsync(stream, ct);
                });

            await _minioClient.GetObjectAsync(getObjectArgs, cancellationToken);

            _logger.LogDebug("Successfully retrieved certificate {ObjectKey}, size: {Size} bytes",
                objectKey, stream.Length);

            return stream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve certificate {ObjectKey}", objectKey);
            throw new InvalidOperationException($"Failed to retrieve certificate {objectKey}: {ex.Message}", ex);
        }
    }

    public async Task DeleteCertificateAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(_config.BucketName)
                .WithObject(objectKey);

            await _minioClient.RemoveObjectAsync(removeObjectArgs, cancellationToken);

            _logger.LogDebug("Successfully deleted certificate {ObjectKey}", objectKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete certificate {ObjectKey}", objectKey);
            throw new InvalidOperationException($"Failed to delete certificate {objectKey}: {ex.Message}", ex);
        }
    }

    public async Task<bool> CertificateExistsAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking existence of ObjectKey: '{ObjectKey}' in bucket '{Bucket}'",
                objectKey, _config.BucketName);

            var statObjectArgs = new StatObjectArgs()
                .WithBucket(_config.BucketName)
                .WithObject(objectKey);

            var objectStat = await _minioClient.StatObjectAsync(statObjectArgs, cancellationToken);

            _logger.LogDebug("Certificate exists: '{ObjectKey}', Size: {Size} bytes, LastModified: {LastModified}",
                objectKey, objectStat.Size, objectStat.LastModified);

            return true;
        }
        catch (Minio.Exceptions.ObjectNotFoundException ex)
        {
            _logger.LogWarning("Certificate does not exist: '{ObjectKey}' - {ErrorMessage}", objectKey, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if certificate {ObjectKey} exists", objectKey);
            throw new InvalidOperationException($"Failed to check if certificate {objectKey} exists: {ex.Message}", ex);
        }
    }

    public async Task EnsureBucketExistsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var bucketExistsArgs = new BucketExistsArgs().WithBucket(_config.BucketName);
            var bucketExists = await _minioClient.BucketExistsAsync(bucketExistsArgs, cancellationToken);

            if (!bucketExists)
            {
                var makeBucketArgs = new MakeBucketArgs().WithBucket(_config.BucketName);
                await _minioClient.MakeBucketAsync(makeBucketArgs, cancellationToken);

                _logger.LogInformation("Created MinIO bucket: {BucketName}", _config.BucketName);
            }
            else
            {
                _logger.LogDebug("MinIO bucket exists: {BucketName}", _config.BucketName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure bucket {BucketName} exists", _config.BucketName);
            throw new InvalidOperationException($"Failed to ensure bucket {_config.BucketName} exists: {ex.Message}", ex);
        }
    }

    public async Task DeleteWorkspaceCertificatesAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_config.UseWorkspacePrefixes)
            {
                _logger.LogWarning("Cannot delete workspace certificates when UseWorkspacePrefixes is disabled");
                return;
            }

            var prefix = $"workspaces/{workspaceId:N}/";
            var listArgs = new ListObjectsArgs()
                .WithBucket(_config.BucketName)
                .WithPrefix(prefix)
                .WithRecursive(true);

            var objectsToDelete = new List<string>();
            await foreach (var item in _minioClient.ListObjectsEnumAsync(listArgs, cancellationToken))
            {
                objectsToDelete.Add(item.Key);
            }

            if (objectsToDelete.Any())
            {
                var successCount = 0;
                var errorCount = 0;

                foreach (var objectKey in objectsToDelete)
                {
                    try
                    {
                        await DeleteCertificateAsync(objectKey, cancellationToken);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to delete object {ObjectKey}", objectKey);
                        errorCount++;
                    }
                }

                _logger.LogInformation("Completed workspace {WorkspaceId} cleanup: {SuccessCount} files deleted, {ErrorCount} errors",
                    workspaceId, successCount, errorCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete certificates for workspace {WorkspaceId}", workspaceId);
            throw new InvalidOperationException($"Failed to delete certificates for workspace {workspaceId}: {ex.Message}", ex);
        }
    }
}