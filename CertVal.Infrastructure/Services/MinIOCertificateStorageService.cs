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
    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

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
        try
        {
            await EnsureBucketExistsAsync(cancellationToken);

            var uniqueFileName = $"{Guid.NewGuid():N}_{SanitizeFileName(fileName)}";
            var objectKey = _config.GetObjectKey(workspaceId, uniqueFileName);

            using var stream = new MemoryStream(fileContent);

            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_config.BucketName)
                .WithObject(objectKey)
                .WithStreamData(stream)
                .WithObjectSize(fileContent.Length)
                .WithContentType("application/octet-stream")
                .WithHeaders(new Dictionary<string, string>
                {
                    ["X-Workspace-Id"] = workspaceId.ToString(),
                    ["X-Original-Filename"] = fileName,
                    ["X-Upload-Date"] = DateTimeOffset.UtcNow.ToString("O")
                });

            await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);

            _logger.LogInformation("Successfully stored certificate {OriginalFileName} as {ObjectKey} for workspace {WorkspaceId}",
                fileName, objectKey, workspaceId);

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
            using var stream = new MemoryStream();

            var getObjectArgs = new GetObjectArgs()
                .WithBucket(_config.BucketName)
                .WithObject(objectKey)
                .WithCallbackStream(async (inputStream, ct) =>
                {
                    await inputStream.CopyToAsync(stream, ct);
                });

            await _minioClient.GetObjectAsync(getObjectArgs, cancellationToken);

            _logger.LogDebug("Successfully retrieved certificate {ObjectKey}", objectKey);

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

            _logger.LogInformation("Successfully deleted certificate {ObjectKey}", objectKey);
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
            var statObjectArgs = new StatObjectArgs()
                .WithBucket(_config.BucketName)
                .WithObject(objectKey);

            await _minioClient.StatObjectAsync(statObjectArgs, cancellationToken);
            return true;
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
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

    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return "unnamed_file";

        var needsSanitization = fileName.AsSpan().IndexOfAny(InvalidFileNameChars) >= 0;
        if (!needsSanitization)
            return fileName;

        var sb = new StringBuilder(fileName.Length);
        foreach (var ch in fileName)
        {
            sb.Append(Array.IndexOf(InvalidFileNameChars, ch) >= 0 ? '_' : ch);
        }

        var sanitized = sb.ToString();

        return string.IsNullOrWhiteSpace(sanitized) ? "unnamed_file" : sanitized;
    }
}