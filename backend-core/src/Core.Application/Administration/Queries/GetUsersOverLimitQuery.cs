using MediatR;
using Core.Application.Administration.DTOs;
using Core.Application.DTOs;

namespace Core.Application.Administration.Queries;

public record GetUsersOverLimitQuery(string LimitKey, int Page = 1, int PageSize = 20) : IRequest<PaginatedResultDto<AdminUserListDto>>;
