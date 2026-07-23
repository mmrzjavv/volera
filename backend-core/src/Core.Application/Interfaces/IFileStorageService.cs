using System.IO;
using System.Threading.Tasks;

namespace Core.Application.Interfaces;

public interface IFileStorageService
{
    bool IsConfigured { get; }

    /// <summary>Uploads bytes and returns the private object key (not a public URL).</summary>
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string? keyPrefix = null);

    /// <summary>Returns a short-lived PUT URL and the object key.</summary>
    (string uploadUrl, string objectKey) GetPresignedUploadUrl(string fileName, string contentType, string? keyPrefix = null);

    /// <summary>Returns a short-lived GET URL for an object key.</summary>
    string GetPresignedDownloadUrl(string objectKey, TimeSpan? lifetime = null);

    /// <summary>If value looks like an object key, return a fresh download URL; legacy http(s) URLs pass through.</summary>
    string? ResolveClientUrl(string? storedUrlOrKey, TimeSpan? lifetime = null);
}
