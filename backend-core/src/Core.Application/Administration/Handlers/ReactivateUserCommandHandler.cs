using MediatR;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Administration.Handlers;

public class ReactivateUserCommandHandler : IRequestHandler<Commands.ReactivateUserCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IAdminAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReactivateUserCommandHandler(
        IUserRepository userRepository,
        IAdminAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(Commands.ReactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId) ?? throw new KeyNotFoundException("User not found.");
        user.Reactivate();
        _userRepository.Update(user);
        await _auditLogRepository.AddAsync(new AdminAuditLog(request.AdminUserId, AdminAuditActions.ReactivateUser, AdminResourceTypes.User, request.UserId));
        await _unitOfWork.SaveChangesAsync();
        return Unit.Value;
    }
}
