using MediatR;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Administration.Handlers;

public class AdminDeleteMessageCommandHandler : IRequestHandler<Commands.AdminDeleteMessageCommand, Unit>
{
    private readonly IMessageRepository _messageRepository;
    private readonly IAdminAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AdminDeleteMessageCommandHandler(
        IMessageRepository messageRepository,
        IAdminAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork)
    {
        _messageRepository = messageRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(Commands.AdminDeleteMessageCommand request, CancellationToken cancellationToken)
    {
        var message = await _messageRepository.GetByIdAsync(request.MessageId) ?? throw new KeyNotFoundException("Message not found.");

        if (request.HardDelete)
        {
            _messageRepository.Delete(message);
        }
        else
        {
            message.Delete();
            _messageRepository.Update(message);
        }

        await _auditLogRepository.AddAsync(new AdminAuditLog(request.AdminUserId, AdminAuditActions.AdminDeleteMessage, AdminResourceTypes.Message, request.MessageId, request.HardDelete ? "HardDelete" : "SoftDelete"));
        await _unitOfWork.SaveChangesAsync();
        return Unit.Value;
    }
}
