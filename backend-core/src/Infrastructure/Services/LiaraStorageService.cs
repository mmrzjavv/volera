using Amazon.S3;
using Amazon.S3.Model;
using Core.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Services;

/// <summary>S3-compatible object storage (MinIO, Liara, AWS). Objects are private by default.</summary>
public class LiaraStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly IAmazonS3 _presignClient;
    private readonly string _bucketName;
    private readonly string _endpointUrl;
    private readonly string _publicEndpointUrl;
    private readonly string _accessKey;
    private readonly string _secretKey;
    private readonly string _region;
    private readonly IServiceProvider _services;
    private readonly Dictionary<string, IAmazonS3> _presignClientsByBase = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _presignLock = new();

    public bool IsConfigured => true;

    public LiaraStorageService(IConfiguration configuration, IServiceProvider services)
    {
        var accessKey = configuration["Storage:AccessKey"];
        var secretKey = configuration["Storage:SecretKey"];
        var bucketName = configuration["Storage:BucketName"];
        var endpointUrl = configuration["Storage:EndpointUrl"];
        var publicEndpoint = configuration["Storage:PublicEndpointUrl"];

        if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(bucketName) || string.IsNullOrEmpty(endpointUrl))
        {
            throw new InvalidOperationException("Storage configuration is incomplete (AccessKey, SecretKey, BucketName, EndpointUrl).");
        }

        _bucketName = bucketName!;
        _accessKey = accessKey!;
        _secretKey = secretKey!;
        _region = configuration["Storage:Region"] ?? "us-east-1";
        _services = services;
        _endpointUrl = NormalizeEndpoint(endpointUrl!, configuration["Storage:UseSsl"]);
        _publicEndpointUrl = string.IsNullOrWhiteSpace(publicEndpoint)
            || string.Equals(publicEndpoint, "auto", StringComparison.OrdinalIgnoreCase)
            ? _endpointUrl
            : NormalizeEndpoint(publicEndpoint!, configuration["Storage:UseSsl"]);

        _s3Client = CreateClient(_accessKey, _secretKey, _endpointUrl, _region);

        _presignClient = string.Equals(_endpointUrl, _publicEndpointUrl, StringComparison.OrdinalIgnoreCase)
            ? _s3Client
            : CreateClient(_accessKey, _secretKey, _publicEndpointUrl, _region);
    }

    private static AmazonS3Client CreateClient(string accessKey, string secretKey, string serviceUrl, string region)
    {
        var config = new AmazonS3Config
        {
            ServiceURL = serviceUrl,
            ForcePathStyle = true,
            AuthenticationRegion = region,
            // Critical for local MinIO: otherwise SDK may emit https:// URLs that browsers cannot reach.
            UseHttp = serviceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        };
        return new AmazonS3Client(accessKey, secretKey, config);
    }

    private static string NormalizeEndpoint(string endpoint, string? useSslRaw)
    {
        if (endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return endpoint.TrimEnd('/');

        var useSsl = !string.Equals(useSslRaw, "false", StringComparison.OrdinalIgnoreCase);
        return (useSsl ? "https://" : "http://") + endpoint.TrimEnd('/');
    }

    private string ResolvePublicBaseUrl()
    {
        var provider = _services.GetService<IPublicStorageEndpointProvider>();
        var fromRequest = provider?.GetPublicBaseUrl();
        if (!string.IsNullOrWhiteSpace(fromRequest))
            return fromRequest.TrimEnd('/');
        return _publicEndpointUrl;
    }

    private IAmazonS3 GetPresignClient(string publicBaseUrl)
    {
        if (string.Equals(publicBaseUrl, _publicEndpointUrl, StringComparison.OrdinalIgnoreCase))
            return _presignClient;

        lock (_presignLock)
        {
            if (_presignClientsByBase.TryGetValue(publicBaseUrl, out var existing))
                return existing;

            var client = CreateClient(_accessKey, _secretKey, publicBaseUrl, _region);
            _presignClientsByBase[publicBaseUrl] = client;
            return client;
        }
    }

    public async Task EnsureBucketExistsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, _bucketName);
            if (!exists)
            {
                await _s3Client.PutBucketAsync(new PutBucketRequest { BucketName = _bucketName }, cancellationToken);
            }
        }
        catch (AmazonS3Exception)
        {
            // Continue; PutObject will surface real errors.
        }
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string? keyPrefix = null)
    {
        var safeName = MediaContentValidator.NormalizeFileName(fileName);
        MediaContentValidator.ValidateOrThrow(safeName, contentType, fileStream.CanSeek ? fileStream.Length : null);
        var resolvedType = MediaContentValidator.ResolveContentType(safeName, contentType);

        await EnsureBucketExistsAsync();

        var key = string.IsNullOrEmpty(keyPrefix)
            ? $"{Guid.NewGuid():N}_{safeName}"
            : $"{keyPrefix.TrimEnd('/')}/{Guid.NewGuid():N}_{safeName}";

        // No CannedACL — MinIO often rejects ACL headers; keep bucket private via MinIO policy.
        var putRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = fileStream,
            ContentType = resolvedType,
            AutoCloseStream = false
        };

        await _s3Client.PutObjectAsync(putRequest);
        return key;
    }

    public (string uploadUrl, string objectKey) GetPresignedUploadUrl(string fileName, string contentType, string? keyPrefix = null)
    {
        var safeName = MediaContentValidator.NormalizeFileName(fileName);
        MediaContentValidator.ValidateOrThrow(safeName, contentType);
        var resolvedType = MediaContentValidator.ResolveContentType(safeName, contentType);

        var key = string.IsNullOrEmpty(keyPrefix)
            ? $"{Guid.NewGuid():N}_{safeName}"
            : $"{keyPrefix.TrimEnd('/')}/{Guid.NewGuid():N}_{safeName}";

        var publicBase = ResolvePublicBaseUrl();
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(15),
            ContentType = resolvedType
        };

        return (FixLocalMinioScheme(GetPresignClient(publicBase).GetPreSignedURL(request), publicBase), key);
    }

    public string GetPresignedDownloadUrl(string objectKey, TimeSpan? lifetime = null)
    {
        if (string.IsNullOrWhiteSpace(objectKey) || objectKey.Contains("..", StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid object key.");

        var publicBase = ResolvePublicBaseUrl();
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = objectKey.TrimStart('/'),
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromMinutes(60))
        };

        return FixLocalMinioScheme(GetPresignClient(publicBase).GetPreSignedURL(request), publicBase);
    }

    /// <summary>
    /// AWSSDK often emits https:// even for http ServiceURL. SigV4 signs the Host header, not the scheme,
    /// so rewriting https→http is safe when the public base is http://...
    /// </summary>
    private static string FixLocalMinioScheme(string url, string publicBaseUrl)
    {
        if (publicBaseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return "http://" + url["https://".Length..];
        }

        return url;
    }

    public string? ResolveClientUrl(string? storedUrlOrKey, TimeSpan? lifetime = null)
    {
        if (string.IsNullOrWhiteSpace(storedUrlOrKey))
            return storedUrlOrKey;

        if (storedUrlOrKey.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || storedUrlOrKey.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            // Legacy absolute URLs: if they point at loopback MinIO, re-sign via object key when possible.
            if (TryExtractObjectKeyFromLegacyUrl(storedUrlOrKey, out var legacyKey))
                return GetPresignedDownloadUrl(legacyKey, lifetime);
            return storedUrlOrKey;
        }

        return GetPresignedDownloadUrl(storedUrlOrKey, lifetime);
    }

    private bool TryExtractObjectKeyFromLegacyUrl(string url, out string objectKey)
    {
        objectKey = string.Empty;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        // path-style: /{bucket}/{key}
        var path = uri.AbsolutePath.TrimStart('/');
        var prefix = _bucketName + "/";
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        objectKey = Uri.UnescapeDataString(path[prefix.Length..]);
        return !string.IsNullOrWhiteSpace(objectKey) && !objectKey.Contains("..", StringComparison.Ordinal);
    }
}
