using System.IO;
using System.Threading.Tasks;
using Core.Application.Interfaces;

namespace Infrastructure.Services;

/// <summary>No-op storage when S3/MinIO is not configured so text messaging can boot.</summary>
public class NullFileStorageService : IFileStorageService
{
    public const string UnavailableMessage = "File storage is not configured or unavailable.";

    public bool IsConfigured => false;

    public Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string? keyPrefix = null)
        => throw new InvalidOperationException(UnavailableMessage);

    public (string uploadUrl, string objectKey) GetPresignedUploadUrl(string fileName, string contentType, string? keyPrefix = null)
        => throw new InvalidOperationException(UnavailableMessage);

    public string GetPresignedDownloadUrl(string objectKey, TimeSpan? lifetime = null)
        => throw new InvalidOperationException(UnavailableMessage);

    public string? ResolveClientUrl(string? storedUrlOrKey, TimeSpan? lifetime = null)
        => storedUrlOrKey;
}
