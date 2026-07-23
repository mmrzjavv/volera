using MediatR;
using Core.Application.Commands;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class SendSupportReplyCommandHandler : IRequestHandler<SendSupportReplyCommand, Guid>
{
    /// <summary>Placeholder User Id used as Message.SenderId for all support-originated messages. Must exist in Users table.</summary>
    public static readonly Guid SystemSupportUserId = new("B1A1A1A1-1111-1111-1111-111111111111");

    private readonly ISupportUserRepository _supportUserRepository;
    private readonly ISupportUserBranchRepository _supportUserBranchRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;

    public SendSupportReplyCommandHandler(
        ISupportUserRepository supportUserRepository,
        ISupportUserBranchRepository supportUserBranchRepository,
        IBranchRepository branchRepository,
        IUserRepository userRepository,
        IMessageRepository messageRepository,
        IUnitOfWork unitOfWork,
        IMediator mediator)
    {
        _supportUserRepository = supportUserRepository;
        _supportUserBranchRepository = supportUserBranchRepository;
        _branchRepository = branchRepository;
        _userRepository = userRepository;
        _messageRepository = messageRepository;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
    }

    public async Task<Guid> Handle(SendSupportReplyCommand request, CancellationToken cancellationToken)
    {
        var supportUser = await _supportUserRepository.GetByIdAsync(request.SupportUserId);
        if (supportUser == null || !supportUser.IsActive)
            throw new UnauthorizedAccessException("Support user not found or inactive.");

        var branch = await _branchRepository.GetByIdAsync(request.BranchId);
        if (branch == null || branch.CompanyId != supportUser.CompanyId)
            throw new InvalidOperationException("Branch not found or access denied.");

        var assignment = await _supportUserBranchRepository.GetBySupportUserIdAndBranchIdAsync(request.SupportUserId, request.BranchId, cancellationToken);
        if (assignment == null && !supportUser.Role.CanViewAllCompanyMessages())
            throw new InvalidOperationException("You are not assigned to this branch.");

        var systemUser = await _userRepository.GetByIdAsync(SystemSupportUserId);
        if (systemUser == null)
            throw new InvalidOperationException("System support user is not configured. Run database seed.");

        var message = new Message(
            SystemSupportUserId,
            supportUser.CompanyId,
            request.BranchId,
            request.SupportUserId,
            request.TargetClientUserId,
            request.Content ?? "",
            request.AttachmentUrl,
            request.AttachmentType,
            request.ReplyToMessageId);

        await _messageRepository.AddAsync(message);
        await _unitOfWork.SaveChangesAsync();

        foreach (var domainEvent in message.DomainEvents)
            await _mediator.Publish(domainEvent, cancellationToken);
        message.ClearDomainEvents();

        return message.Id;
    }
}
