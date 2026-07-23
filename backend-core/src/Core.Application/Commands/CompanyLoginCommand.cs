using MediatR;

namespace Core.Application.Commands;

/// <summary>
/// Company login with registration token. Demo OTP only when AllowDemoOtp is set by the host (never by clients).
/// </summary>
public class CompanyLoginCommand : IRequest<CompanyLoginResult?>
{
    public required string MobileNumber { get; set; }
    /// <summary>Registration token (or demo OTP when host enables AllowDemoOtp).</summary>
    public required string Token { get; set; }
    /// <summary>Host-controlled. Must never be set from client request body.</summary>
    public bool AllowDemoOtp { get; set; }
    /// <summary>Host-controlled demo OTP value when AllowDemoOtp is true.</summary>
    public string? DemoOtpValue { get; set; }
}

public class CompanyLoginResult
{
    public Guid CompanyId { get; init; }
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
}
