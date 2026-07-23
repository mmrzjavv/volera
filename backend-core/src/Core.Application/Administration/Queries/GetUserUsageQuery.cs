using MediatR;
using Core.Application.Administration.DTOs;
using Core.Application.DTOs;

namespace Core.Application.Administration.Queries;

public record GetUserUsageQuery(
    int Page = 1,
    int PageSize = 20,
    string? SortBy = null,
    bool SortDesc = true) : IRequest<PaginatedResultDto<UserUsageDto>>;
