using Core.Application.Commands;
using Core.Domain.Interfaces;
using MediatR;

namespace Core.Application.Handlers;

public class DeleteGroupCommandHandler : IRequestHandler<DeleteGroupCommand>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteGroupCommandHandler(IGroupRepository groupRepository, IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteGroupCommand request, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetGroupWithMembersAsync(request.GroupId);
        if (group == null)
            throw new KeyNotFoundException("Group not found.");

        var isAdmin = group.AdminId == request.RequestingUserId
            || group.Members.Any(m => m.UserId == request.RequestingUserId && m.IsAdmin);

        if (!isAdmin)
            throw new UnauthorizedAccessException("Only group admins can delete the group.");

        _groupRepository.Delete(group);
        await _unitOfWork.SaveChangesAsync();
    }
}
