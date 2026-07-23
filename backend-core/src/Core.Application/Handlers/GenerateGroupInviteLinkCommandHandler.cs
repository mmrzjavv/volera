using Core.Application.Commands;
using Core.Domain.Interfaces;
using MediatR;

namespace Core.Application.Handlers;

public class GenerateGroupInviteLinkCommandHandler : IRequestHandler<GenerateGroupInviteLinkCommand, string>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GenerateGroupInviteLinkCommandHandler(IGroupRepository groupRepository, IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<string> Handle(GenerateGroupInviteLinkCommand request, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetByIdAsync(request.GroupId);
        if (group == null)
            throw new KeyNotFoundException("Group not found.");

        if (group.AdminId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Only the group admin can generate invite links.");

        group.EnsureInviteCode();
        _groupRepository.Update(group);
        await _unitOfWork.SaveChangesAsync();

        return group.InviteCode!;
    }
}

