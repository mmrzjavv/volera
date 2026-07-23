using MediatR;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Administration.Handlers;

public class SuspendUserCommandHandler : IRequestHandler<Commands.SuspendUserCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IAdminAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SuspendUserCommandHandler(
        IUserRepository userRepository,
        IAdminAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(Commands.SuspendUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId) ?? throw new KeyNotFoundException("User not found.");
        user.Suspend(request.Until, request.AdminUserId);
        _userRepository.Update(user);
        await _auditLogRepository.AddAsync(new AdminAuditLog(request.AdminUserId, AdminAuditActions.SuspendUser, AdminResourceTypes.User, request.UserId, request.Until.ToString("O")));
        await _unitOfWork.SaveChangesAsync();
        return Unit.Value;
    }
}
