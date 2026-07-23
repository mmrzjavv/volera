using Core.Application.DTOs;
using Core.Application.Interfaces;
using Core.Application.Queries;
using Core.Domain.Interfaces;
using MediatR;

namespace Core.Application.Handlers;

public class GetGroupByInviteCodeQueryHandler : IRequestHandler<GetGroupByInviteCodeQuery, GroupDetailsDto>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IFileStorageService _fileStorage;

    public GetGroupByInviteCodeQueryHandler(IGroupRepository groupRepository, IFileStorageService fileStorage)
    {
        _groupRepository = groupRepository;
        _fileStorage = fileStorage;
    }

    public async Task<GroupDetailsDto> Handle(GetGroupByInviteCodeQuery request, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetByInviteCodeAsync(request.InviteCode);
        if (group == null)
            throw new KeyNotFoundException("Invalid invite code.");

        return new GroupDetailsDto
        {
            Id = group.Id,
            Name = group.Name,
            AdminId = group.AdminId,
            CreatedAt = group.CreatedAt,
            Description = group.Description,
            ProfilePictureUrl = _fileStorage.ResolveClientUrl(group.ProfilePictureUrl),
            InviteCode = group.InviteCode,
            Members = new List<UserDto>()
        };
    }
}
