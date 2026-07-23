using MediatR;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Administration.Handlers;

public class AdminUpdateUserCommandHandler : IRequestHandler<Commands.AdminUpdateUserCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IAdminAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AdminUpdateUserCommandHandler(
        IUserRepository userRepository,
        IAdminAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(Commands.AdminUpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId) ?? throw new KeyNotFoundException("User not found.");
        user.UpdateProfile(request.FirstName, request.LastName, null, request.Email, request.Bio);
        _userRepository.Update(user);
        await _auditLogRepository.AddAsync(new AdminAuditLog(request.AdminUserId, AdminAuditActions.AdminUpdateUser, AdminResourceTypes.User, request.UserId));
        await _unitOfWork.SaveChangesAsync();
        return Unit.Value;
    }
}
