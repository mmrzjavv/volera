using MediatR;
using Core.Application.Commands;
using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Core.Application.Interfaces;

namespace Core.Application.Handlers;

public class CreateSupportUserCommandHandler : IRequestHandler<CreateSupportUserCommand, Guid>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ISupportUserRepository _supportUserRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSupportUserCommandHandler(
        ICompanyRepository companyRepository,
        ISupportUserRepository supportUserRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _companyRepository = companyRepository;
        _supportUserRepository = supportUserRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateSupportUserCommand request, CancellationToken cancellationToken)
    {
        var company = await _companyRepository.GetByIdAsync(request.CompanyId);
        if (company == null)
            throw new InvalidOperationException("Company not found.");

        var existing = await _supportUserRepository.GetByCompanyIdAndUsernameAsync(request.CompanyId, request.Username, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException("A support user with this username already exists in the company.");

        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var supportUser = new SupportUser(
            request.CompanyId,
            request.Username.Trim(),
            passwordHash,
            request.FirstName.Trim(),
            request.LastName.Trim(),
            request.Role,
            string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim());

        await _supportUserRepository.AddAsync(supportUser);
        await _unitOfWork.SaveChangesAsync();
        return supportUser.Id;
    }
}
