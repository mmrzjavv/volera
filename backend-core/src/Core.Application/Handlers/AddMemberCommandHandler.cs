using MediatR;
using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Core.Application.Commands;

namespace Core.Application.Handlers;

public class AddMemberCommandHandler : IRequestHandler<AddMemberCommand>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddMemberCommandHandler(IGroupRepository groupRepository, IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AddMemberCommand request, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetGroupWithMembersAsync(request.GroupId);
        if (group == null)
            throw new KeyNotFoundException("Group not found.");

        // Check permissions
        var adminMember = group.Members.FirstOrDefault(m => m.UserId == request.AdminId);
        if (adminMember == null || !adminMember.IsAdmin)
        {
            throw new UnauthorizedAccessException("Only admins can add members.");
        }

        var newMember = group.AddMember(request.MemberId, false);
        if (newMember == null)
            return; // already a member

        _groupRepository.AddMember(newMember);
        await _unitOfWork.SaveChangesAsync();
    }
}
