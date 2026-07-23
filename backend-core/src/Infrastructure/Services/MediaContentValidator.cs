namespace Infrastructure.Services;

/// <summary>MIME/extension allowlist and size limits. Never trust client Content-Type alone.</summary>
public static class MediaContentValidator
{
    public const long MaxUploadBytes = 50 * 1024 * 1024; // 50 MB

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "audio/mpeg", "audio/ogg", "audio/webm", "audio/wav", "audio/mp4", "audio/x-wav", "audio/wave",
        "video/mp4", "video/webm", "video/ogg",
        "application/pdf",
        "text/plain",
        "text/csv",
        "application/csv",
        "application/vnd.ms-excel",
        "application/zip",
        "application/octet-stream"
    };

    private static readonly Dictionary<string, string> ExtensionToContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".mp3"] = "audio/mpeg",
        [".ogg"] = "audio/ogg",
        [".oga"] = "audio/ogg",
        [".wav"] = "audio/wav",
        [".webm"] = "video/webm", // may also be audio/webm from MediaRecorder
        [".mp4"] = "video/mp4",
        [".m4a"] = "audio/mp4",
        [".pdf"] = "application/pdf",
        [".txt"] = "text/plain",
        [".csv"] = "text/csv",
        [".zip"] = "application/zip"
    };

    public static void ValidateOrThrow(string fileName, string? contentType, long? lengthBytes = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new InvalidOperationException("File name is required.");

        if (lengthBytes is > MaxUploadBytes)
            throw new InvalidOperationException($"File exceeds maximum size of {MaxUploadBytes / (1024 * 1024)} MB.");

        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(ext) || !ExtensionToContentType.ContainsKey(ext))
            throw new InvalidOperationException("File type is not allowed.");

        var normalizedType = string.IsNullOrWhiteSpace(contentType)
            ? ExtensionToContentType[ext]
            : contentType.Split(';')[0].Trim();

        if (!AllowedContentTypes.Contains(normalizedType))
            throw new InvalidOperationException("Content type is not allowed.");

        if (string.Equals(normalizedType, "application/octet-stream", StringComparison.OrdinalIgnoreCase))
            return;

        var expected = ExtensionToContentType[ext];
        if (string.Equals(normalizedType, expected, StringComparison.OrdinalIgnoreCase))
            return;

        // Browsers may send CSV as application/csv or application/vnd.ms-excel.
        if (string.Equals(ext, ".csv", StringComparison.OrdinalIgnoreCase)
            && (normalizedType is "text/csv" or "application/csv" or "application/vnd.ms-excel" or "text/plain"))
            return;

        // MediaRecorder often produces audio/webm or audio/ogg with .webm/.ogg extensions.
        if (IsCompatibleMediaPair(normalizedType, expected, ext))
            return;

        throw new InvalidOperationException("File extension does not match content type.");
    }

    private static bool IsCompatibleMediaPair(string actual, string expected, string ext)
    {
        var actualMajor = actual.Split('/')[0];
        var expectedMajor = expected.Split('/')[0];

        if (string.Equals(ext, ".webm", StringComparison.OrdinalIgnoreCase)
            && (actual is "audio/webm" or "video/webm"))
            return true;

        if (string.Equals(ext, ".ogg", StringComparison.OrdinalIgnoreCase)
            && (actual.StartsWith("audio/", StringComparison.OrdinalIgnoreCase)
                || actual.StartsWith("video/", StringComparison.OrdinalIgnoreCase)))
            return true;

        if (string.Equals(ext, ".mp4", StringComparison.OrdinalIgnoreCase)
            && (actual is "audio/mp4" or "video/mp4"))
            return true;

        return string.Equals(actualMajor, expectedMajor, StringComparison.OrdinalIgnoreCase)
               && (actualMajor is "audio" or "video" or "image");
    }

    public static string NormalizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "file.bin" : name;
    }

    public static string ResolveContentType(string fileName, string? contentType)
    {
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            var normalized = contentType.Split(';')[0].Trim();
            if (AllowedContentTypes.Contains(normalized)
                && !string.Equals(normalized, "application/octet-stream", StringComparison.OrdinalIgnoreCase))
                return normalized;
        }

        var ext = Path.GetExtension(fileName);
        if (ExtensionToContentType.TryGetValue(ext, out var mapped))
            return mapped;
        return "application/octet-stream";
    }
}
