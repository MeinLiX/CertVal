using System.ComponentModel.DataAnnotations;

namespace CertVal.Application.Configuration;

public class CertificateStorageConfiguration
{
    public const string SectionName = "CertificateStorage";

    [Required]
    public string BucketName { get; set; } = "certificates";

    public bool UseWorkspacePrefixes { get; set; } = true;

    public bool DeleteOnCertificateRemoval { get; set; } = true;

    public long MaxFileSize { get; set; } = 10 * 1024 * 1024;

    public string GetObjectKey(Guid workspaceId, string fileName)
    {
        if (UseWorkspacePrefixes)
        {
            return $"workspaces/{workspaceId:N}/{fileName}";
        }
        return fileName;
    }
}