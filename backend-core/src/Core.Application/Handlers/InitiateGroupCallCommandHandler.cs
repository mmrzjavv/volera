using MediatR;
using Core.Application.Commands;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class InitiateGroupCallCommandHandler : IRequestHandler<InitiateGroupCallCommand, Guid>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupCallRepository _groupCallRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGroupCallNotificationService _groupCallNotificationService;

    public InitiateGroupCallCommandHandler(
        IGroupRepository groupRepository,
        IGroupCallRepository groupCallRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IGroupCallNotificationService groupCallNotificationService)
    {
        _groupRepository = groupRepository;
        _groupCallRepository = groupCallRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _groupCallNotificationService = groupCallNotificationService;
    }

    public async Task<Guid> Handle(InitiateGroupCallCommand request, CancellationToken cancellationToken)
    {
        // Load group with members to validate membership and gather recipients
        var group = await _groupRepository.GetGroupWithMembersAsync(request.GroupId)
            ?? throw new KeyNotFoundException("Group not found.");

        if (group.Members.All(m => m.UserId != request.InitiatorId))
        {
            throw new UnauthorizedAccessException("You are not a member of this group.");
        }

        // Prevent multiple active calls per group
        var activeCall = await _groupCallRepository.GetActiveByGroupIdAsync(request.GroupId);
        if (activeCall != null)
        {
            // For now, just return the existing active call id so the client can join it
            return activeCall.Id;
        }

        // Create the new group call aggregate
        var groupCall = new GroupCall(request.GroupId, request.InitiatorId, request.IsVideo);
        await _groupCallRepository.AddAsync(groupCall);
        await _unitOfWork.SaveChangesAsync();

        // Resolve initiator name for nicer notifications (fallback to empty if missing)
        var initiator = await _userRepository.GetByIdAsync(request.InitiatorId);
        var initiatorName = initiator != null
            ? $"{initiator.FirstName} {initiator.LastName}".Trim()
            : string.Empty;

        // Collect member user ids to notify (excluding or including initiator is fine; include all)
        var memberUserIds = group.Members.Select(m => m.UserId).Distinct().ToList();

        await _groupCallNotificationService.SendGroupCallInitiated(
            groupCall.Id,
            request.GroupId,
            request.InitiatorId,
            initiatorName,
            request.IsVideo,
            memberUserIds);

        return groupCall.Id;
    }
}

