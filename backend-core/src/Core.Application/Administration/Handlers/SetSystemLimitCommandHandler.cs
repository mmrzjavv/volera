using MediatR;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Administration.Handlers;

public class SetSystemLimitCommandHandler : IRequestHandler<Commands.SetSystemLimitCommand, Unit>
{
    private readonly ISystemLimitRepository _systemLimitRepository;
    private readonly IAdminAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetSystemLimitCommandHandler(
        ISystemLimitRepository systemLimitRepository,
        IAdminAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork)
    {
        _systemLimitRepository = systemLimitRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(Commands.SetSystemLimitCommand request, CancellationToken cancellationToken)
    {
        var existing = await _systemLimitRepository.GetByKeyAsync(request.LimitKey, cancellationToken);
        if (existing != null)
        {
            existing.SetValue(request.Value);
            _systemLimitRepository.Update(existing);
        }
        else
        {
            var limit = new SystemLimit(request.LimitKey, request.Value);
            await _systemLimitRepository.AddAsync(limit);
        }

        await _auditLogRepository.AddAsync(new AdminAuditLog(request.AdminUserId, AdminAuditActions.SetSystemLimit, AdminResourceTypes.Limit, null, $"{request.LimitKey}={request.Value}"));
        await _unitOfWork.SaveChangesAsync();
        return Unit.Value;
    }
}
