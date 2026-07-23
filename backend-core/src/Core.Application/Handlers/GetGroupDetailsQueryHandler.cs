using Core.Application.DTOs;
using Core.Application.Interfaces;
using Core.Application.Queries;
using Core.Domain.Interfaces;
using MediatR;
using AutoMapper;

namespace Core.Application.Handlers;

public class GetGroupDetailsQueryHandler : IRequestHandler<GetGroupDetailsQuery, GroupDetailsDto>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMapper _mapper;
    private readonly IFileStorageService _fileStorage;

    public GetGroupDetailsQueryHandler(
        IGroupRepository groupRepository,
        IMapper mapper,
        IFileStorageService fileStorage)
    {
        _groupRepository = groupRepository;
        _mapper = mapper;
        _fileStorage = fileStorage;
    }

    public async Task<GroupDetailsDto> Handle(GetGroupDetailsQuery request, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetGroupWithMembersAsync(request.GroupId);
        if (group == null)
            throw new KeyNotFoundException("Group not found.");

        if (!group.Members.Any(m => m.UserId == request.CurrentUserId))
            throw new UnauthorizedAccessException("You are not a member of this group.");

        var dto = new GroupDetailsDto
        {
            Id = group.Id,
            Name = group.Name,
            AdminId = group.AdminId,
            CreatedAt = group.CreatedAt,
            Description = group.Description,
            ProfilePictureUrl = _fileStorage.ResolveClientUrl(group.ProfilePictureUrl),
            InviteCode = group.InviteCode,
            Members = group.Members
                .OrderBy(m => m.JoinedAt)
                .Select(m =>
                {
                    var user = _mapper.Map<UserDto>(m.User);
                    user.ProfilePicture = _fileStorage.ResolveClientUrl(user.ProfilePicture);
                    return user;
                })
                .ToList()
        };

        return dto;
    }
}
