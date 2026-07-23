using MediatR;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Administration.Handlers;

public class AdminEditMessageCommandHandler : IRequestHandler<Commands.AdminEditMessageCommand, Unit>
{
    private readonly IMessageRepository _messageRepository;
    private readonly IAdminAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AdminEditMessageCommandHandler(
        IMessageRepository messageRepository,
        IAdminAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork)
    {
        _messageRepository = messageRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(Commands.AdminEditMessageCommand request, CancellationToken cancellationToken)
    {
        var message = await _messageRepository.GetByIdAsync(request.MessageId) ?? throw new KeyNotFoundException("Message not found.");
        message.Edit(request.NewContent);
        _messageRepository.Update(message);
        await _auditLogRepository.AddAsync(new AdminAuditLog(request.AdminUserId, AdminAuditActions.AdminEditMessage, AdminResourceTypes.Message, request.MessageId, request.NewContent));
        await _unitOfWork.SaveChangesAsync();
        return Unit.Value;
    }
}
