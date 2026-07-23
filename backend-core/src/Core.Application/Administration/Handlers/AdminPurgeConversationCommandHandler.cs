using MediatR;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Administration.Handlers;

public class AdminPurgeConversationCommandHandler : IRequestHandler<Commands.AdminPurgeConversationCommand, int>
{
    private readonly IMessageRepository _messageRepository;
    private readonly IAdminAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AdminPurgeConversationCommandHandler(
        IMessageRepository messageRepository,
        IAdminAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork)
    {
        _messageRepository = messageRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(Commands.AdminPurgeConversationCommand request, CancellationToken cancellationToken)
    {
        var parts = request.ConversationKey.Split('_');
        int deleted = 0;
        if (parts.Length >= 2 && parts[0].Equals("group", StringComparison.OrdinalIgnoreCase) && Guid.TryParse(parts[1], out var gId))
        {
            deleted = await _messageRepository.DeleteByConversationAsync(null, null, gId, cancellationToken);
        }
        else if (parts.Length >= 3 && parts[0].Equals("dm", StringComparison.OrdinalIgnoreCase) &&
            Guid.TryParse(parts[1], out var u1) && Guid.TryParse(parts[2], out var u2))
        {
            deleted = await _messageRepository.DeleteByConversationAsync(u1, u2, null, cancellationToken);
        }

        if (deleted > 0)
        {
            await _auditLogRepository.AddAsync(new AdminAuditLog(request.AdminUserId, AdminAuditActions.AdminPurgeConversation, AdminResourceTypes.Chat, null, $"{request.ConversationKey};{deleted} deleted"));
            await _unitOfWork.SaveChangesAsync();
        }
        return deleted;
    }
}
