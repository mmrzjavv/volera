using Core.Application.Commands;
using Core.Domain.Interfaces;
using MediatR;

namespace Core.Application.Handlers;

public class LeaveGroupCommandHandler : IRequestHandler<LeaveGroupCommand>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LeaveGroupCommandHandler(IGroupRepository groupRepository, IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(LeaveGroupCommand request, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetGroupWithMembersAsync(request.GroupId);
        if (group == null)
            throw new KeyNotFoundException("Group not found.");

        var member = group.Members.FirstOrDefault(m => m.UserId == request.UserId);
        if (member == null)
        {
            // Not a member – nothing to do
            return;
        }

        var isAdminLeaving = group.AdminId == request.UserId;

        if (isAdminLeaving)
        {
            // Try to promote another member to admin before leaving
            var newAdmin = group.Members
                .Where(m => m.UserId != request.UserId)
                .OrderBy(m => m.JoinedAt)
                .FirstOrDefault();

            if (newAdmin != null)
            {
                group.ChangeAdmin(newAdmin.UserId);
            }
        }

        group.RemoveMember(request.UserId);
        _groupRepository.Update(group);
        await _unitOfWork.SaveChangesAsync();
    }
}

