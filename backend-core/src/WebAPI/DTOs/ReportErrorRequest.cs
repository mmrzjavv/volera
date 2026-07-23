using System.ComponentModel.DataAnnotations;

namespace WebAPI.DTOs;

/// <summary>
/// Request body for POST /api/errors (frontend error reporting).
/// </summary>
public class ReportErrorRequest
{
    [Required]
    [MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(10000)]
    public string? StackTrace { get; set; }

    [MaxLength(2000)]
    public string? Url { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [MaxLength(5000)]
    public string? ComponentStack { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }
}
