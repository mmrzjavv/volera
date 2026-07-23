using MediatR;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Administration.Handlers;

public class RemoveUserLimitOverrideCommandHandler : IRequestHandler<Commands.RemoveUserLimitOverrideCommand, Unit>
{
    private readonly IUserLimitOverrideRepository _overrideRepository;
    private readonly IAdminAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveUserLimitOverrideCommandHandler(
        IUserLimitOverrideRepository overrideRepository,
        IAdminAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork)
    {
        _overrideRepository = overrideRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(Commands.RemoveUserLimitOverrideCommand request, CancellationToken cancellationToken)
    {
        var existing = await _overrideRepository.GetAsync(request.UserId, request.LimitKey, cancellationToken);
        if (existing != null)
        {
            _overrideRepository.Delete(existing);
            await _auditLogRepository.AddAsync(new AdminAuditLog(request.AdminUserId, AdminAuditActions.RemoveUserLimitOverride, AdminResourceTypes.Limit, request.UserId, request.LimitKey));
            await _unitOfWork.SaveChangesAsync();
        }
        return Unit.Value;
    }
}
