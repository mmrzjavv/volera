using MediatR;
using Core.Application.Commands;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class RegisterCompanyCommandHandler : IRequestHandler<RegisterCompanyCommand, RegisterCompanyResult>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ICompanyTokenService _companyTokenService;
    private readonly IRefreshTokenHasher _hasher;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterCompanyCommandHandler(
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

    public async Task<RegisterCompanyResult> Handle(RegisterCompanyCommand request, CancellationToken cancellationToken)
    {
        var existing = await _companyRepository.GetByMobileNumberAsync(request.MobileNumber, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException("A company with this mobile number is already registered.");

        var company = new Company(
            request.Name.Trim(),
            request.MobileNumber.Trim(),
            string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim());

        await _companyRepository.AddAsync(company);
        await _unitOfWork.SaveChangesAsync();

        var token = _companyTokenService.GenerateSecureToken();
        var tokenHash = _hasher.Hash(token);
        var expiresAt = DateTime.UtcNow.AddHours(24);
        company.SetRegistrationToken(tokenHash, expiresAt);
        _companyRepository.Update(company);
        await _unitOfWork.SaveChangesAsync();

        return new RegisterCompanyResult
        {
            CompanyId = company.Id,
            Token = token,
            ExpiresAt = expiresAt
        };
    }
}
