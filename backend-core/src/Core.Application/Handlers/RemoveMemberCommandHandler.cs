using MediatR;
using Core.Domain.Interfaces;
using Core.Application.Commands;

namespace Core.Application.Handlers;

public class RemoveMemberCommandHandler : IRequestHandler<RemoveMemberCommand>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveMemberCommandHandler(IGroupRepository groupRepository, IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetGroupWithMembersAsync(request.GroupId);
        if (group == null)
            throw new KeyNotFoundException("Group not found.");

        // Check that the caller is an admin in this group
        var adminMember = group.Members.FirstOrDefault(m => m.UserId == request.AdminId);
        if (adminMember == null || !adminMember.IsAdmin)
        {
            throw new UnauthorizedAccessException("Only admins can remove members.");
        }

        // Do not allow removing the current group admin
        if (request.MemberId == group.AdminId)
        {
            throw new InvalidOperationException("Cannot remove the group admin. Change admin first, then remove.");
        }

        // If the target user is not a member, nothing to do
        if (!group.Members.Any(m => m.UserId == request.MemberId))
        {
            return;
        }

        group.RemoveMember(request.MemberId);
        _groupRepository.Update(group);
        await _unitOfWork.SaveChangesAsync();
    }
}

