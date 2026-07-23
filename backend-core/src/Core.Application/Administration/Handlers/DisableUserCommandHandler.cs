using MediatR;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Administration.Handlers;

public class DisableUserCommandHandler : IRequestHandler<Commands.DisableUserCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IAdminAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DisableUserCommandHandler(
        IUserRepository userRepository,
        IAdminAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(Commands.DisableUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId) ?? throw new KeyNotFoundException("User not found.");
        user.Disable(request.AdminUserId);
        _userRepository.Update(user);
        await _auditLogRepository.AddAsync(new AdminAuditLog(request.AdminUserId, AdminAuditActions.DisableUser, AdminResourceTypes.User, request.UserId));
        await _unitOfWork.SaveChangesAsync();
        return Unit.Value;
    }
}
