using Core.Application.DTOs;
using Core.Application.Queries;
using Core.Domain.Interfaces;
using MediatR;
using AutoMapper;

namespace Core.Application.Handlers;

public class GetGroupDetailsQueryHandler : IRequestHandler<GetGroupDetailsQuery, GroupDetailsDto>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMapper _mapper;

    public GetGroupDetailsQueryHandler(IGroupRepository groupRepository, IMapper mapper)
    {
        _groupRepository = groupRepository;
        _mapper = mapper;
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
            ProfilePictureUrl = group.ProfilePictureUrl,
            InviteCode = group.InviteCode,
            Members = group.Members
                .OrderBy(m => m.JoinedAt)
                .Select(m => _mapper.Map<UserDto>(m.User))
                .ToList()
        };

        return dto;
    }
}

