using Core.Application.DTOs;
using MediatR;

namespace Core.Application.Queries;

public class GetGroupByInviteCodeQuery : IRequest<GroupDetailsDto>
{
    public string InviteCode { get; set; } = string.Empty;
}

