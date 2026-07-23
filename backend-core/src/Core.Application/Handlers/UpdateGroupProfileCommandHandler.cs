using Core.Application.Commands;
using Core.Domain.Interfaces;
using MediatR;

namespace Core.Application.Handlers;

public class UpdateGroupProfileCommandHandler : IRequestHandler<UpdateGroupProfileCommand>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateGroupProfileCommandHandler(IGroupRepository groupRepository, IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateGroupProfileCommand request, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetGroupWithMembersAsync(request.GroupId);
        if (group == null)
            throw new KeyNotFoundException("Group not found.");

        if (group.AdminId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Only the group admin can update the profile.");

        group.UpdateProfile(request.Name, request.Description, request.ProfilePictureUrl);
        _groupRepository.Update(group);
        await _unitOfWork.SaveChangesAsync();
    }
}

