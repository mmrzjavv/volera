using MediatR;
using Core.Application.Administration.DTOs;
using Core.Application.DTOs;

namespace Core.Application.Administration.Queries;

public record GetAdminChatListQuery(
    int Page = 1,
    int PageSize = 20,
    string? SearchTerm = null,
    string? TypeFilter = null) : IRequest<PaginatedResultDto<AdminChatDto>>;
