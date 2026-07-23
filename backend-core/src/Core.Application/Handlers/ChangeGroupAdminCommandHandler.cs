using Core.Application.Commands;
using Core.Domain.Interfaces;
using MediatR;

namespace Core.Application.Handlers;

public class ChangeGroupAdminCommandHandler : IRequestHandler<ChangeGroupAdminCommand>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeGroupAdminCommandHandler(IGroupRepository groupRepository, IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ChangeGroupAdminCommand request, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetGroupWithMembersAsync(request.GroupId);
        if (group == null)
            throw new KeyNotFoundException("Group not found.");

        if (group.AdminId != request.CurrentAdminId)
            throw new UnauthorizedAccessException("Only the current admin can change the group admin.");

        if (!group.Members.Any(m => m.UserId == request.NewAdminId))
            throw new InvalidOperationException("New admin must be a member of the group.");

        group.ChangeAdmin(request.NewAdminId);
        _groupRepository.Update(group);
        await _unitOfWork.SaveChangesAsync();
    }
}

