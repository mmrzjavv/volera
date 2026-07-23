using MediatR;
using Core.Application.Commands;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class CreateCompanyClientSessionCommandHandler : IRequestHandler<CreateCompanyClientSessionCommand, CreateCompanyClientSessionResult?>
{
    private readonly ICompanyWidgetRepository _widgetRepository;
    private readonly ICompanyClientRepository _companyClientRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICompanyWidgetTokenService _widgetTokenService;
    private readonly IRefreshTokenHasher _hasher;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    private const string CompanyClientPasswordPlaceholder = "company-client-no-login";

    public CreateCompanyClientSessionCommandHandler(
        ICompanyWidgetRepository widgetRepository,
        ICompanyClientRepository companyClientRepository,
        IUserRepository userRepository,
        ICompanyWidgetTokenService widgetTokenService,
        IRefreshTokenHasher hasher,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _widgetRepository = widgetRepository;
        _companyClientRepository = companyClientRepository;
        _userRepository = userRepository;
        _widgetTokenService = widgetTokenService;
        _hasher = hasher;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateCompanyClientSessionResult?> Handle(CreateCompanyClientSessionCommand request, CancellationToken cancellationToken)
    {
        var widget = await _widgetRepository.GetByWidgetIdAsync(request.WidgetId, cancellationToken);
        if (widget == null || !widget.IsActive)
            return null;

        var prefix = Guid.NewGuid().ToString("N")[..10];
        var username = $"c_{prefix}";
        var phoneNumber = $"c{prefix}";
        var passwordHash = _passwordHasher.HashPassword(CompanyClientPasswordPlaceholder);
        var user = new User(
            request.FirstName ?? "",
            request.LastName ?? "",
            username,
            phoneNumber,
            passwordHash,
            UserRole.CompanyClient);

        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var token = _widgetTokenService.GenerateSecureToken();
        var tokenHash = _hasher.Hash(token);
        var expiresAt = DateTime.UtcNow.AddHours(24);
        var client = new CompanyClient(
            widget.CompanyId,
            widget.BranchId,
            widget.Id,
            user.Id,
            tokenHash,
            expiresAt,
            string.IsNullOrWhiteSpace(request.FirstName) ? null : request.FirstName.Trim(),
            string.IsNullOrWhiteSpace(request.LastName) ? null : request.LastName.Trim(),
            string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            string.IsNullOrWhiteSpace(request.Mobile) ? null : request.Mobile.Trim());

        await _companyClientRepository.AddAsync(client);
        await _unitOfWork.SaveChangesAsync();

        return new CreateCompanyClientSessionResult
        {
            ClientToken = token,
            ClientId = client.Id,
            ExpiresAt = expiresAt
        };
    }
}
