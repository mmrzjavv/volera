using Core.Application.Commands;
using Core.Domain.Interfaces;
using MediatR;

namespace Core.Application.Handlers;

public class JoinGroupByInviteCommandHandler : IRequestHandler<JoinGroupByInviteCommand>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public JoinGroupByInviteCommandHandler(IGroupRepository groupRepository, IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(JoinGroupByInviteCommand request, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetByInviteCodeAsync(request.InviteCode);
        if (group == null)
            throw new KeyNotFoundException("Invalid invite code.");

        var newMember = group.AddMember(request.UserId, isAdmin: false);
        if (newMember != null)
        {
            _groupRepository.AddMember(newMember);
        }
        await _unitOfWork.SaveChangesAsync();
    }
}

