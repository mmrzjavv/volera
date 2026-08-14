using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Core.Application.Commands;
using Core.Application.Queries;
using Core.Application.Logging;
using WebAPI.Extensions;
using WebAPI.Models;
using Core.Application.Interfaces;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/v1/company")]
public class CompanyController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICompanyTokenService _companyTokenService;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<CompanyController> _logger;

    public const string CompanyTokenHeaderName = "X-Company-Token";

    public CompanyController(
        IMediator mediator,
        ICompanyTokenService companyTokenService,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<CompanyController> logger)
    {
        _mediator = mediator;
        _companyTokenService = companyTokenService;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>Register a company. Returns token for subsequent API calls. OTP verification is TODO.</summary>
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCompanyRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterCompanyCommand
        {
            Name = request.Name,
            MobileNumber = request.MobileNumber,
            Email = request.Email,
            Address = request.Address
        };
        var result = await _mediator.Send(command, cancellationToken);
        AppLog.Info(_logger, AppLogEvents.CompanyRegistered,
            "CompanyId: {CompanyId} | Mobile: {Mobile} | Result: Success",
            result.CompanyId, request.MobileNumber);
        return new ObjectResult(ApiResponse<object>.Ok(new
        {
            companyId = result.CompanyId,
            token = result.Token,
            expiresAt = result.ExpiresAt
        })) { StatusCode = 201 };
    }

    /// <summary>Company login with registration token. Demo OTP only when Auth:AllowDemoCompanyOtp is true in Development.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] CompanyLoginRequest request, CancellationToken cancellationToken)
    {
        var allowDemo = _environment.IsDevelopment()
            && _configuration.GetValue("Auth:AllowDemoCompanyOtp", false);
        var command = new CompanyLoginCommand
        {
            MobileNumber = request.MobileNumber,
            Token = request.Token,
            AllowDemoOtp = allowDemo,
            DemoOtpValue = allowDemo ? _configuration["Auth:DemoCompanyOtp"] : null
        };
        var result = await _mediator.Send(command, cancellationToken);
        if (result == null)
        {
            AppLog.Warning(_logger, AppLogEvents.CompanyLoginFailed,
                "Mobile: {Mobile} | IP: {ClientIp} | Reason: InvalidCredentials | Result: Failure",
                request.MobileNumber, HttpContext.Connection.RemoteIpAddress?.ToString());
            return this.ApiUnauthorized("Invalid mobile number or token.");
        }

        AppLog.Info(_logger, AppLogEvents.CompanyLoginSucceeded,
            "CompanyId: {CompanyId} | Mobile: {Mobile} | IP: {ClientIp} | Method: Token | Result: Success",
            result.CompanyId, request.MobileNumber, HttpContext.Connection.RemoteIpAddress?.ToString());
        return this.Success(new
        {
            companyId = result.CompanyId,
            token = result.Token,
            expiresAt = result.ExpiresAt
        });
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyIdFromTokenAsync(cancellationToken);
        if (companyId == null) return this.ApiUnauthorized("Valid company token required. Send X-Company-Token header.");
        var query = new GetCompanyByIdQuery { CompanyId = companyId.Value };
        var company = await _mediator.Send(query, cancellationToken);
        if (company == null) return NotFound();
        return this.Success(company);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateCompanyRequest request, CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyIdFromTokenAsync(cancellationToken);
        if (companyId == null) return this.ApiUnauthorized("Valid company token required. Send X-Company-Token header.");
        var command = new UpdateCompanyCommand
        {
            CompanyId = companyId.Value,
            Name = request.Name,
            Email = request.Email,
            Address = request.Address,
            LogoUrl = request.LogoUrl
        };
        await _mediator.Send(command, cancellationToken);
        return this.Success();
    }

    [HttpGet("branches")]
    public async Task<IActionResult> GetBranches(CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyIdFromTokenAsync(cancellationToken);
        if (companyId == null) return this.ApiUnauthorized("Valid company token required. Send X-Company-Token header.");
        var query = new GetCompanyBranchesQuery { CompanyId = companyId.Value };
        var branches = await _mediator.Send(query, cancellationToken);
        return this.Success(branches);
    }

    [HttpPost("branches")]
    public async Task<IActionResult> CreateBranch([FromBody] CreateBranchRequest request, CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyIdFromTokenAsync(cancellationToken);
        if (companyId == null) return this.ApiUnauthorized("Valid company token required. Send X-Company-Token header.");
        var command = new CreateBranchCommand
        {
            CompanyId = companyId.Value,
            Name = request.Name,
            Address = request.Address,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email
        };
        var branchId = await _mediator.Send(command, cancellationToken);
        return new ObjectResult(ApiResponse<object>.Ok(new { branchId })) { StatusCode = 201 };
    }

    [HttpPut("branches/{id:guid}")]
    public async Task<IActionResult> UpdateBranch(Guid id, [FromBody] UpdateBranchRequest request, CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyIdFromTokenAsync(cancellationToken);
        if (companyId == null) return this.ApiUnauthorized("Valid company token required. Send X-Company-Token header.");
        var command = new UpdateBranchCommand
        {
            BranchId = id,
            CompanyId = companyId.Value,
            Name = request.Name,
            Address = request.Address,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email
        };
        await _mediator.Send(command, cancellationToken);
        return this.Success();
    }

    [HttpDelete("branches/{id:guid}")]
    public async Task<IActionResult> DeleteBranch(Guid id, CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyIdFromTokenAsync(cancellationToken);
        if (companyId == null) return this.ApiUnauthorized("Valid company token required. Send X-Company-Token header.");
        var command = new DeleteBranchCommand { BranchId = id, CompanyId = companyId.Value };
        await _mediator.Send(command, cancellationToken);
        return this.Success();
    }

    private async Task<Guid?> GetCompanyIdFromTokenAsync(CancellationToken cancellationToken)
    {
        var token = Request.Headers[CompanyTokenHeaderName].FirstOrDefault();
        if (string.IsNullOrEmpty(token)) return null;
        var company = await _companyTokenService.ValidateTokenAsync(token, cancellationToken);
        return company?.Id;
    }
}

public class RegisterCompanyRequest
{
    public string Name { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
}

public class CompanyLoginRequest
{
    public string MobileNumber { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}

public class UpdateCompanyRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? LogoUrl { get; set; }
}

public class CreateBranchRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
}

public class UpdateBranchRequest
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
}
