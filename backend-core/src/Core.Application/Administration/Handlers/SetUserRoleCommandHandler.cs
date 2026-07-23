using MediatR;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Administration.Handlers;

public class SetUserRoleCommandHandler : IRequestHandler<Commands.SetUserRoleCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IAdminAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetUserRoleCommandHandler(
        IUserRepository userRepository,
        IAdminAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(Commands.SetUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId) ?? throw new KeyNotFoundException("User not found.");
        var superAdminCount = await _userRepository.CountSuperAdminsAsync(cancellationToken);
        var role = UserRoleExtensions.FromName(request.Role);
        user.SetRole(role, superAdminCount);
        _userRepository.Update(user);
        await _auditLogRepository.AddAsync(new AdminAuditLog(request.AdminUserId, AdminAuditActions.SetRole, AdminResourceTypes.User, request.UserId, request.Role));
        await _unitOfWork.SaveChangesAsync();
        return Unit.Value;
    }
}
