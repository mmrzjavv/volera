using MediatR;
using Core.Application.Commands;
using Core.Application.Interfaces;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class CompanyLoginCommandHandler : IRequestHandler<CompanyLoginCommand, CompanyLoginResult?>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ICompanyTokenService _companyTokenService;
    private readonly IRefreshTokenHasher _hasher;
    private readonly IUnitOfWork _unitOfWork;

    public CompanyLoginCommandHandler(
        ICompanyRepository companyRepository,
        ICompanyTokenService companyTokenService,
        IRefreshTokenHasher hasher,
        IUnitOfWork unitOfWork)
    {
        _companyRepository = companyRepository;
        _companyTokenService = companyTokenService;
        _hasher = hasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<CompanyLoginResult?> Handle(CompanyLoginCommand request, CancellationToken cancellationToken)
    {
        var company = await _companyRepository.GetByMobileNumberAsync(request.MobileNumber.Trim(), cancellationToken);
        if (company == null || !company.IsActive)
            return null;

        var token = request.Token?.Trim() ?? string.Empty;

        // Demo OTP is explicitly opt-in (dev only). Never enabled by default.
        if (request.AllowDemoOtp
            && !string.IsNullOrEmpty(request.DemoOtpValue)
            && token == request.DemoOtpValue)
        {
            var newToken = _companyTokenService.GenerateSecureToken();
            var tokenHash = _hasher.Hash(newToken);
            var expiresAt = DateTime.UtcNow.AddHours(24);
            company.SetRegistrationToken(tokenHash, expiresAt);
            _companyRepository.Update(company);
            await _unitOfWork.SaveChangesAsync();
            return new CompanyLoginResult
            {
                CompanyId = company.Id,
                Token = newToken,
                ExpiresAt = expiresAt
            };
        }

        var validated = await _companyTokenService.ValidateTokenAsync(token, cancellationToken);
        if (validated == null || validated.Id != company.Id)
            return null;

        return new CompanyLoginResult
        {
            CompanyId = company.Id,
            Token = token,
            ExpiresAt = validated.TokenExpiresAt ?? DateTime.UtcNow
        };
    }
}
