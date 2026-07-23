using Core.Application.DTOs;
using Core.Application.Queries;
using Core.Domain.Enums;
using Core.Domain.Interfaces;
using MediatR;

namespace Core.Application.Handlers;

public class GetUserGroupsQueryHandler : IRequestHandler<GetUserGroupsQuery, List<GroupDto>>
{
    private readonly IGroupRepository _groupRepository;

    public GetUserGroupsQueryHandler(IGroupRepository groupRepository)
    {
        _groupRepository = groupRepository;
    }

    public async Task<List<GroupDto>> Handle(GetUserGroupsQuery request, CancellationToken cancellationToken)
    {
        var groups = await _groupRepository.GetGroupsForUserAsync(request.UserId);
        return groups.Select(g => new GroupDto
        {
            Id = g.Id,
            Name = g.Name,
            AdminId = g.AdminId,
            CreatedAt = g.CreatedAt,
            Kind = g.Kind.ToString(),
            IsChannel = g.Kind == GroupKind.Channel,
            ProfilePictureUrl = g.ProfilePictureUrl,
            IsPublic = g.IsPublic,
            PublicUsername = g.PublicUsername
        }).ToList();
    }
}
