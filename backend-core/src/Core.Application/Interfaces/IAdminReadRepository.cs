using Core.Application.Administration.DTOs;

namespace Core.Application.Interfaces;

public interface IAdminReadRepository
{
    Task<(IEnumerable<AdminUserListDto> Items, int TotalCount)> GetAdminUserListAsync(
        int page,
        int pageSize,
        string? searchTerm,
        string? roleFilter,
        bool? isDisabledFilter,
        string? sortBy,
        bool sortDesc,
        CancellationToken cancellationToken = default);

    Task<AdminUserDetailDto?> GetAdminUserDetailAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<(IEnumerable<AdminChatDto> Items, int TotalCount)> GetAdminChatListAsync(
        int page,
        int pageSize,
        string? searchTerm,
        string? typeFilter,
        CancellationToken cancellationToken = default);

    Task<(IEnumerable<AdminMessageDto> Items, int TotalCount)> SearchMessagesAsync(
        int page,
        int pageSize,
        string? contentSearch,
        Guid? senderId,
        Guid? groupId,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default);

    Task<SystemStatsDto> GetSystemStatsAsync(CancellationToken cancellationToken = default);

    Task<(IEnumerable<AdminUserListDto> Items, int TotalCount)> GetUsersOverLimitAsync(string limitKey, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<(IEnumerable<AdminChatMessageDto> Messages, DateTime? NextCursor, bool HasMore, string Title, string Type)> GetConversationMessagesAsync(
        string conversationKey, int limit, DateTime? beforeCursor, CancellationToken cancellationToken = default);

    Task<ExtendedMonitoringStatsDto> GetExtendedMonitoringStatsAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<MessagesPerDayDto>> GetMessagesPerDayAsync(int days, CancellationToken cancellationToken = default);

    Task<(IEnumerable<MostActiveUserDto> Items, int TotalCount)> GetMostActiveUsersAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<(IEnumerable<MostActiveGroupDto> Items, int TotalCount)> GetMostActiveGroupsAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<TableRowCountsDto> GetTableRowCountsAsync(CancellationToken cancellationToken = default);

    Task<(IEnumerable<UserUsageDto> Items, int TotalCount)> GetUserUsageAsync(int page, int pageSize, string? sortBy, bool sortDesc, CancellationToken cancellationToken = default);

    Task<int> GetUnreadMessagesCountAsync(CancellationToken cancellationToken = default);

    Task<AdminChatDto?> GetChatByKeyAsync(string conversationKey, CancellationToken cancellationToken = default);
}
