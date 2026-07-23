using System.Security.Cryptography;
using MediatR;
using Core.Application.Commands;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class GenerateCompanyWidgetCommandHandler : IRequestHandler<GenerateCompanyWidgetCommand, GenerateCompanyWidgetResult>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly ICompanyWidgetRepository _widgetRepository;
    private readonly IRefreshTokenHasher _hasher;
    private readonly IUnitOfWork _unitOfWork;

    public GenerateCompanyWidgetCommandHandler(
        ICompanyRepository companyRepository,
        IBranchRepository branchRepository,
        ICompanyWidgetRepository widgetRepository,
        IRefreshTokenHasher hasher,
        IUnitOfWork unitOfWork)
    {
        _companyRepository = companyRepository;
        _branchRepository = branchRepository;
        _widgetRepository = widgetRepository;
        _hasher = hasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<GenerateCompanyWidgetResult> Handle(GenerateCompanyWidgetCommand request, CancellationToken cancellationToken)
    {
        var company = await _companyRepository.GetByIdAsync(request.CompanyId);
        if (company == null)
            throw new InvalidOperationException("Company not found.");
        var branch = await _branchRepository.GetByIdAsync(request.BranchId);
        if (branch == null || branch.CompanyId != request.CompanyId)
            throw new InvalidOperationException("Branch not found.");

        var widgetId = "w_" + GenerateShortId();
        var token = GenerateSecureToken();
        var tokenHash = _hasher.Hash(token);
        var widget = new CompanyWidget(request.CompanyId, request.BranchId, widgetId, tokenHash);
        await _widgetRepository.AddAsync(widget);
        await _unitOfWork.SaveChangesAsync();

        return new GenerateCompanyWidgetResult
        {
            WidgetEntityId = widget.Id,
            WidgetId = widgetId,
            WidgetToken = token
        };
    }

    private static string GenerateShortId()
    {
        var bytes = new byte[12];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").TrimEnd('=')[..16];
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
