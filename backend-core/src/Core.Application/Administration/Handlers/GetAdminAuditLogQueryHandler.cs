using MediatR;
using Core.Application.Administration.DTOs;
using Core.Application.Administration.Queries;
using Core.Application.DTOs;
using Core.Domain.Interfaces;

namespace Core.Application.Administration.Handlers;

public class GetAdminAuditLogQueryHandler : IRequestHandler<GetAdminAuditLogQuery, PaginatedResultDto<AdminAuditLogDto>>
{
    private readonly IAdminAuditLogRepository _auditLogRepository;
    private readonly IUserRepository _userRepository;

    public GetAdminAuditLogQueryHandler(IAdminAuditLogRepository auditLogRepository, IUserRepository userRepository)
    {
        _auditLogRepository = auditLogRepository;
        _userRepository = userRepository;
    }

    public async Task<PaginatedResultDto<AdminAuditLogDto>> Handle(GetAdminAuditLogQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _auditLogRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.AdminUserId,
            request.Action,
            request.ResourceType,
            request.From,
            request.To,
            cancellationToken);

        var adminIds = items.Select(a => a.AdminUserId).Distinct().ToList();
        var users = adminIds.Count > 0
            ? (await _userRepository.GetUsersByIdsAsync(adminIds, cancellationToken)).ToDictionary(u => u.Id, u => u.Username)
            : new Dictionary<Guid, string>();

        var dtos = items.Select(a => new AdminAuditLogDto
        {
            Id = a.Id,
            AdminUserId = a.AdminUserId,
            AdminUsername = users.GetValueOrDefault(a.AdminUserId),
            Action = a.Action,
            ResourceType = a.ResourceType,
            ResourceId = a.ResourceId,
            Details = a.Details,
            CreatedAt = a.CreatedAt
        }).ToList();

        return new PaginatedResultDto<AdminAuditLogDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
