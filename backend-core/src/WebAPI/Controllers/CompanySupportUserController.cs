using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Core.Application.Commands;
using Core.Application.Queries;
using WebAPI.Extensions;
using WebAPI.Models;
using Core.Application.Interfaces;

namespace WebAPI.Controllers;

/// <summary>
/// Support user management using company token (X-Company-Token).
/// For use by the company admin panel when logged in with company credentials.
/// </summary>
[ApiController]
[Route("api/v1/company/support-users")]
public class CompanySupportUserController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICompanyTokenService _companyTokenService;
    private readonly ILogger<CompanySupportUserController> _logger;

    public CompanySupportUserController(
        IMediator mediator,
        ICompanyTokenService companyTokenService,
        ILogger<CompanySupportUserController> logger)
    {
        _mediator = mediator;
        _companyTokenService = companyTokenService;
        _logger = logger;
    }

    private async Task<Guid?> GetCompanyIdFromTokenAsync(CancellationToken cancellationToken)
    {
        var token = Request.Headers[CompanyController.CompanyTokenHeaderName].FirstOrDefault();
        if (string.IsNullOrEmpty(token)) return null;
        var company = await _companyTokenService.ValidateTokenAsync(token, cancellationToken);
        return company?.Id;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyIdFromTokenAsync(cancellationToken);
        if (companyId == null) return this.ApiUnauthorized("Valid company token required. Send X-Company-Token header.");
        var query = new GetSupportUsersByCompanyQuery { CompanyId = companyId.Value };
        var users = await _mediator.Send(query, cancellationToken);
        return this.Success(users);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSupportUserRequest request, CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyIdFromTokenAsync(cancellationToken);
        if (companyId == null) return this.ApiUnauthorized("Valid company token required. Send X-Company-Token header.");
        var command = new CreateSupportUserCommand
        {
            CompanyId = companyId.Value,
            Username = request.Username,
            Password = request.Password,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = request.Role,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber
        };
        var id = await _mediator.Send(command, cancellationToken);
        return new ObjectResult(ApiResponse<object>.Ok(new { supportUserId = id })) { StatusCode = 201 };
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSupportUserRequest request, CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyIdFromTokenAsync(cancellationToken);
        if (companyId == null) return this.ApiUnauthorized("Valid company token required. Send X-Company-Token header.");
        var command = new UpdateSupportUserCommand
        {
            SupportUserId = id,
            CompanyId = companyId.Value,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber
        };
        await _mediator.Send(command, cancellationToken);
        return this.Success();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyIdFromTokenAsync(cancellationToken);
        if (companyId == null) return this.ApiUnauthorized("Valid company token required. Send X-Company-Token header.");
        var command = new DeleteSupportUserCommand { SupportUserId = id, CompanyId = companyId.Value };
        await _mediator.Send(command, cancellationToken);
        return this.Success();
    }

    [HttpPost("{id:guid}/assign-branch")]
    public async Task<IActionResult> AssignBranch(Guid id, [FromBody] AssignBranchRequest request, CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyIdFromTokenAsync(cancellationToken);
        if (companyId == null) return this.ApiUnauthorized("Valid company token required. Send X-Company-Token header.");
        var command = new AssignSupportUserToBranchCommand
        {
            SupportUserId = id,
            BranchId = request.BranchId,
            CompanyId = companyId.Value
        };
        await _mediator.Send(command, cancellationToken);
        return this.Success();
    }

    [HttpDelete("{id:guid}/assign-branch/{branchId:guid}")]
    public async Task<IActionResult> UnassignBranch(Guid id, Guid branchId, CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyIdFromTokenAsync(cancellationToken);
        if (companyId == null) return this.ApiUnauthorized("Valid company token required. Send X-Company-Token header.");
        var command = new UnassignSupportUserFromBranchCommand
        {
            SupportUserId = id,
            BranchId = branchId,
            CompanyId = companyId.Value
        };
        await _mediator.Send(command, cancellationToken);
        return this.Success();
    }

    [HttpGet("{id:guid}/branches")]
    public async Task<IActionResult> GetBranches(Guid id, CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyIdFromTokenAsync(cancellationToken);
        if (companyId == null) return this.ApiUnauthorized("Valid company token required. Send X-Company-Token header.");
        var query = new GetSupportUserBranchesQuery { SupportUserId = id };
        var branches = await _mediator.Send(query, cancellationToken);
        return this.Success(branches);
    }
}
