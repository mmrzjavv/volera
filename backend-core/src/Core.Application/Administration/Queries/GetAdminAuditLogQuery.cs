using MediatR;
using Core.Application.Administration.DTOs;
using Core.Application.DTOs;

namespace Core.Application.Administration.Queries;

public record GetAdminAuditLogQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? AdminUserId = null,
    string? Action = null,
    string? ResourceType = null,
    DateTime? From = null,
    DateTime? To = null) : IRequest<PaginatedResultDto<AdminAuditLogDto>>;
