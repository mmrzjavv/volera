using MediatR;
using Core.Application.Administration.DTOs;
using Core.Application.DTOs;

namespace Core.Application.Administration.Queries;

public record GetAdminUserListQuery(
    int Page = 1,
    int PageSize = 20,
    string? SearchTerm = null,
    string? RoleFilter = null,
    bool? IsDisabledFilter = null,
    string? SortBy = null,
    bool SortDesc = false) : IRequest<PaginatedResultDto<AdminUserListDto>>;
