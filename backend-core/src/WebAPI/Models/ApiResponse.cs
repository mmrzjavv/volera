using System.Text.Json.Serialization;

namespace WebAPI.Models;

/// <summary>
/// Standard API response envelope for all endpoints.
/// </summary>
public class ApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("operationDate")]
    public DateTime OperationDate { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Message { get; set; }

    public static ApiResponse<T> Ok(T? data) => new()
    {
        Success = true,
        OperationDate = DateTime.UtcNow,
        Data = data
    };

    /// <summary>Single message.</summary>
    public static ApiResponse<object?> Fail(string? message = null) => new()
    {
        Success = false,
        OperationDate = DateTime.UtcNow,
        Data = null,
        Message = string.IsNullOrEmpty(message) ? null : new[] { message }
    };

    /// <summary>Multiple messages (e.g. validation errors).</summary>
    public static ApiResponse<object?> Fail(IReadOnlyList<string>? messages) => new()
    {
        Success = false,
        OperationDate = DateTime.UtcNow,
        Data = null,
        Message = messages is { Count: > 0 } ? messages : null
    };
}
