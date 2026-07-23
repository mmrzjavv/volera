using MediatR;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Administration.Handlers;

public class SetUserLimitOverrideCommandHandler : IRequestHandler<Commands.SetUserLimitOverrideCommand, Unit>
{
    private readonly IUserLimitOverrideRepository _overrideRepository;
    private readonly IAdminAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetUserLimitOverrideCommandHandler(
        IUserLimitOverrideRepository overrideRepository,
        IAdminAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork)
    {
        _overrideRepository = overrideRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(Commands.SetUserLimitOverrideCommand request, CancellationToken cancellationToken)
    {
        var existing = await _overrideRepository.GetAsync(request.UserId, request.LimitKey, cancellationToken);
        if (existing != null)
        {
            existing.SetValue(request.Value);
            _overrideRepository.Update(existing);
        }
        else
        {
            var override_ = new UserLimitOverride(request.UserId, request.LimitKey, request.Value);
            await _overrideRepository.AddAsync(override_);
        }

        await _auditLogRepository.AddAsync(new AdminAuditLog(request.AdminUserId, AdminAuditActions.SetUserLimitOverride, AdminResourceTypes.Limit, request.UserId, $"{request.LimitKey}={request.Value}"));
        await _unitOfWork.SaveChangesAsync();
        return Unit.Value;
    }
}
