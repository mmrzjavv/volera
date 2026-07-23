using MediatR;
using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Core.Application.Commands;

namespace Core.Application.Handlers;

public class CreateGroupCommandHandler : IRequestHandler<CreateGroupCommand, Guid>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateGroupCommandHandler(IGroupRepository groupRepository, IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateGroupCommand request, CancellationToken cancellationToken)
    {
        // Create group
        var group = new Group(request.Name, request.CreatorId);
        
        // Add creator as admin
        group.AddMember(request.CreatorId, true);

        // Add other members (verify they exist)
        foreach (var memberId in request.MemberIds.Distinct())
        {
            if (memberId == request.CreatorId)
                continue;

            var member = await _userRepository.GetByIdAsync(memberId);
            if (member == null)
                throw new KeyNotFoundException($"User not found: {memberId}");

            group.AddMember(memberId, false);
        }

        await _groupRepository.AddAsync(group);
        await _unitOfWork.SaveChangesAsync();

        return group.Id;
    }
}
