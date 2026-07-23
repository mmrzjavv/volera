using Core.Application.Administration.DTOs;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class AdminReadRepository : IAdminReadRepository
{
    private readonly ApplicationDbContext _context;

    public AdminReadRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<AdminUserListDto> Items, int TotalCount)> GetAdminUserListAsync(
        int page,
        int pageSize,
        string? searchTerm,
        string? roleFilter,
        bool? isDisabledFilter,
        string? sortBy,
        bool sortDesc,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(u =>
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term) ||
                u.Username.ToLower().Contains(term) ||
                u.PhoneNumber.Contains(term) ||
                (u.Email != null && u.Email.ToLower().Contains(term)));
        }
        if (!string.IsNullOrWhiteSpace(roleFilter))
        {
            var role = UserRoleExtensions.FromName(roleFilter);
            query = query.Where(u => u.Role == role);
        }
        if (isDisabledFilter.HasValue)
            query = query.Where(u => u.IsDisabled == isDisabledFilter.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var ordered = sortBy?.ToLowerInvariant() switch
        {
            "username" => sortDesc ? query.OrderByDescending(u => u.Username) : query.OrderBy(u => u.Username),
            "role" => sortDesc ? query.OrderByDescending(u => u.Role) : query.OrderBy(u => u.Role),
            "createdat" => sortDesc ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
            _ => sortDesc ? query.OrderByDescending(u => u.FirstName).ThenByDescending(u => u.LastName) : query.OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
        };

        var users = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new { u.Id })
            .ToListAsync(cancellationToken);

        var ids = users.Select(u => u.Id).ToList();
        if (ids.Count == 0)
            return (Enumerable.Empty<AdminUserListDto>(), totalCount);

        var messageCounts = await _context.Messages.AsNoTracking()
            .Where(m => ids.Contains(m.SenderId))
            .GroupBy(m => m.SenderId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);

        var savedCounts = await _context.SavedMessages.AsNoTracking()
            .Where(s => ids.Contains(s.UserId))
            .GroupBy(s => s.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);

        var groupMemberCounts = await _context.GroupMembers.AsNoTracking()
            .Where(gm => ids.Contains(gm.UserId))
            .GroupBy(gm => gm.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);

        var dmPairsRaw = await _context.Messages.AsNoTracking()
            .Where(m => m.GroupId == null && m.ReceiverId != null && (ids.Contains(m.SenderId) || ids.Contains(m.ReceiverId!.Value)))
            .Select(m => new { m.SenderId, m.ReceiverId })
            .ToListAsync(cancellationToken);
        var dmCountByUser = ids.ToDictionary(id => id, _ => 0);
        foreach (var id in ids)
        {
            var partners = dmPairsRaw
                .Where(p => p.SenderId == id || p.ReceiverId == id)
                .Select(p => p.SenderId == id ? p.ReceiverId : p.SenderId)
                .Where(r => r.HasValue)
                .Select(r => r!.Value)
                .Distinct()
                .Count();
            dmCountByUser[id] = partners;
        }

        var userList = await _context.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToListAsync(cancellationToken);

        var items = userList.Select(u => new AdminUserListDto
        {
            Id = u.Id,
            Username = u.Username,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Role = u.Role.ToRoleName(),
            IsDisabled = u.IsDisabled,
            SuspendedUntil = u.SuspendedUntil,
            CreatedAt = u.CreatedAt,
            MessageCount = messageCounts.GetValueOrDefault(u.Id, 0),
            ChatCount = dmCountByUser.GetValueOrDefault(u.Id, 0) + groupMemberCounts.GetValueOrDefault(u.Id, 0),
            SavedMessagesCount = savedCounts.GetValueOrDefault(u.Id, 0),
            StorageUsedBytes = 0
        }).ToList();

        return (items, totalCount);
    }

    public async Task<AdminUserDetailDto?> GetAdminUserDetailAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null) return null;

        var messageCount = await _context.Messages.AsNoTracking().CountAsync(m => m.SenderId == userId, cancellationToken);
        var savedCount = await _context.SavedMessages.AsNoTracking().CountAsync(s => s.UserId == userId, cancellationToken);
        var dmCount = await _context.Messages.AsNoTracking()
            .Where(m => m.GroupId == null && (m.SenderId == userId || m.ReceiverId == userId))
            .Select(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
            .Distinct()
            .CountAsync(cancellationToken);
        var groupCount = await _context.GroupMembers.AsNoTracking().CountAsync(gm => gm.UserId == userId, cancellationToken);

        var overrides = await _context.UserLimitOverrides.AsNoTracking()
            .Where(o => o.UserId == userId)
            .Select(o => new AdminLimitOverrideDto { LimitKey = o.LimitKey, Value = o.Value })
            .ToListAsync(cancellationToken);

        return new AdminUserDetailDto
        {
            Id = user.Id,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email,
            Bio = user.Bio,
            ProfilePicture = user.ProfilePicture,
            Role = user.Role.ToRoleName(),
            IsDisabled = user.IsDisabled,
            SuspendedUntil = user.SuspendedUntil,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            MessageCount = messageCount,
            ChatCount = dmCount + groupCount,
            SavedMessagesCount = savedCount,
            StorageUsedBytes = 0,
            LimitOverrides = overrides
        };
    }

    public async Task<(IEnumerable<AdminChatDto> Items, int TotalCount)> GetAdminChatListAsync(
        int page,
        int pageSize,
        string? searchTerm,
        string? typeFilter,
        CancellationToken cancellationToken = default)
    {
        var chats = new List<AdminChatDto>();

        if (typeFilter != "Group")
        {
            var dmBaseQuery = _context.Messages.AsNoTracking()
                .Where(m => m.GroupId == null && m.ReceiverId != null);
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                dmBaseQuery = dmBaseQuery.Where(m =>
                    _context.Users.Any(u => (u.Id == m.SenderId || u.Id == m.ReceiverId) &&
                        (EF.Functions.ILike(u.FirstName + " " + u.LastName, "%" + term + "%") || EF.Functions.ILike(u.Username, "%" + term + "%"))));
            }
            var dmOrderedQuery = dmBaseQuery
                .GroupBy(m => new { m.SenderId, m.ReceiverId })
                .Select(g => new
                {
                    g.Key.SenderId,
                    g.Key.ReceiverId,
                    LastContent = g.OrderByDescending(x => x.SentAt).Select(x => x.Content).FirstOrDefault(),
                    LastSentAt = g.Max(x => x.SentAt)
                })
                .OrderByDescending(x => x.LastSentAt);

            var totalDms = await dmOrderedQuery.CountAsync(cancellationToken);
            var dmPage = await dmOrderedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var dmUserIds = dmPage.SelectMany(d => new[] { d.SenderId, d.ReceiverId!.Value }).Distinct().ToList();
            var dmUsers = dmUserIds.Count == 0
                ? new Dictionary<Guid, string>()
                : await _context.Users.AsNoTracking()
                    .Where(u => dmUserIds.Contains(u.Id))
                    .Select(u => new { u.Id, DisplayName = (u.FirstName + " " + u.LastName).Trim() != "" ? (u.FirstName + " " + u.LastName).Trim() : u.Username })
                    .ToDictionaryAsync(x => x.Id, x => x.DisplayName, cancellationToken);
            foreach (var dm in dmPage)
            {
                var u1 = dmUsers.TryGetValue(dm.SenderId, out var n1) ? n1 : null;
                var u2 = dm.ReceiverId.HasValue && dmUsers.TryGetValue(dm.ReceiverId.Value, out var n2) ? n2 : null;
                chats.Add(new AdminChatDto
                {
                    ConversationKey = $"dm_{dm.SenderId}_{dm.ReceiverId}",
                    Type = "Dm",
                    UserId1 = dm.SenderId,
                    UserId2 = dm.ReceiverId,
                    UserName1 = u1,
                    UserName2 = u2,
                    LastMessageContent = dm.LastContent,
                    LastMessageAt = dm.LastSentAt
                });
            }
            if (typeFilter == "Dm")
                return (chats, totalDms);
        }

        if (typeFilter != "Dm")
        {
            var groupQuery = _context.Groups.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                groupQuery = groupQuery.Where(g => g.Name.ToLower().Contains(term));
            }
            var groups = await groupQuery
                .OrderByDescending(g => g.CreatedAt)
                .Skip(typeFilter == "Group" ? (page - 1) * pageSize : 0)
                .Take(typeFilter == "Group" ? pageSize : 50)
                .Select(g => new { g.Id, g.Name })
                .ToListAsync(cancellationToken);

            var groupIds = groups.Select(g => g.Id).ToList();
            Dictionary<Guid, (string? Content, DateTime SentAt)> lastMessages;
            if (groupIds.Count > 0)
            {
                var lastMessagesData = await _context.Messages.AsNoTracking()
                    .Where(m => m.GroupId != null && groupIds.Contains(m.GroupId!.Value))
                    .GroupBy(m => m.GroupId)
                    .Select(g => new { GroupId = g.Key!.Value, Content = g.OrderByDescending(x => x.SentAt).Select(x => x.Content).FirstOrDefault(), SentAt = g.Max(x => x.SentAt) })
                    .ToListAsync(cancellationToken);
                lastMessages = lastMessagesData.ToDictionary(x => x.GroupId, x => (x.Content, x.SentAt));
            }
            else
            {
                lastMessages = new Dictionary<Guid, (string? Content, DateTime SentAt)>();
            }

            foreach (var g in groups)
            {
                var hasLast = lastMessages.TryGetValue(g.Id, out var lm);
                var lastContent = hasLast ? lm.Content : (string?)null;
                var lastSentAt = hasLast ? lm.SentAt : DateTime.MinValue;
                chats.Add(new AdminChatDto
                {
                    ConversationKey = $"group_{g.Id}",
                    Type = "Group",
                    GroupId = g.Id,
                    GroupName = g.Name,
                    LastMessageContent = lastContent,
                    LastMessageAt = lastSentAt != DateTime.MinValue ? lastSentAt : (DateTime?)null
                });
            }
            if (typeFilter == "Group")
            {
                var totalGroups = await _context.Groups.AsNoTracking().CountAsync(cancellationToken);
                return (chats, totalGroups);
            }
        }

        return (chats, chats.Count);
    }

    public async Task<(IEnumerable<AdminMessageDto> Items, int TotalCount)> SearchMessagesAsync(
        int page,
        int pageSize,
        string? contentSearch,
        Guid? senderId,
        Guid? groupId,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Messages.AsNoTracking().Include(m => m.Sender).AsQueryable();

        if (!string.IsNullOrWhiteSpace(contentSearch))
        {
            var term = contentSearch.ToLower();
            query = query.Where(m => m.Content.ToLower().Contains(term));
        }
        if (senderId.HasValue)
            query = query.Where(m => m.SenderId == senderId.Value);
        if (groupId.HasValue)
            query = query.Where(m => m.GroupId == groupId);
        if (dateFrom.HasValue)
            query = query.Where(m => m.SentAt >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(m => m.SentAt <= dateTo.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new AdminMessageDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                ReceiverId = m.ReceiverId,
                GroupId = m.GroupId,
                Content = m.Content,
                AttachmentUrl = m.AttachmentUrl,
                AttachmentType = m.AttachmentType,
                SentAt = m.SentAt,
                IsEdited = m.IsEdited,
                DeletedAt = m.DeletedAt,
                SenderUsername = m.Sender.Username
            })
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<SystemStatsDto> GetSystemStatsAsync(CancellationToken cancellationToken = default)
    {
        var totalUsers = await _context.Users.CountAsync(cancellationToken);
        var totalMessages = await _context.Messages.CountAsync(cancellationToken);
        var totalGroups = await _context.Groups.CountAsync(cancellationToken);
        return new SystemStatsDto
        {
            TotalUsers = totalUsers,
            TotalMessages = totalMessages,
            TotalGroups = totalGroups,
            StorageUsedBytes = 0,
            UsersOverLimit = 0
        };
    }

    public Task<(IEnumerable<AdminUserListDto> Items, int TotalCount)> GetUsersOverLimitAsync(string limitKey, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        // Placeholder: when over-limit logic is implemented, query users exceeding the limit and use Skip/Take here.
        return Task.FromResult<(IEnumerable<AdminUserListDto>, int)>((Enumerable.Empty<AdminUserListDto>(), 0));
    }

    public async Task<(IEnumerable<AdminChatMessageDto> Messages, DateTime? NextCursor, bool HasMore, string Title, string Type)> GetConversationMessagesAsync(
        string conversationKey, int limit, DateTime? beforeCursor, CancellationToken cancellationToken = default)
    {
        limit = Math.Min(Math.Max(limit, 1), 100);
        var parts = conversationKey.Split('_');
        if (parts.Length < 2) return (Enumerable.Empty<AdminChatMessageDto>(), null, false, "", "");

        if (parts[0].Equals("dm", StringComparison.OrdinalIgnoreCase) && parts.Length >= 3 &&
            Guid.TryParse(parts[1], out var u1) && Guid.TryParse(parts[2], out var u2))
        {
            var query = _context.Messages.AsNoTracking()
                .Where(m => m.GroupId == null && (
                    (m.SenderId == u1 && m.ReceiverId == u2) || (m.SenderId == u2 && m.ReceiverId == u1)));
            if (beforeCursor.HasValue) query = query.Where(m => m.SentAt < beforeCursor.Value);
            var messages = await query
                .OrderByDescending(m => m.SentAt)
                .Take(limit + 1)
                .Include(m => m.Sender)
                .ToListAsync(cancellationToken);
            var hasMore = messages.Count > limit;
            var page = messages.Take(limit).OrderBy(m => m.SentAt).ToList();
            var nextCursor = hasMore ? page.Last().SentAt : (DateTime?)null;
            var userNames = await _context.Users.AsNoTracking()
                .Where(u => u.Id == u1 || u.Id == u2)
                .Select(u => new { u.Id, u.Username, u.FirstName, u.LastName })
                .ToDictionaryAsync(x => x.Id, cancellationToken);
            var dtos = page.Select(m => new AdminChatMessageDto
            {
                Id = m.Id, SenderId = m.SenderId, ReceiverId = m.ReceiverId, GroupId = null,
                Content = m.Content, AttachmentUrl = m.AttachmentUrl, AttachmentType = m.AttachmentType,
                SentAt = m.SentAt, IsEdited = m.IsEdited, DeletedAt = m.DeletedAt, IsFromMe = false,
                SenderUsername = m.Sender?.Username ?? "", SenderFirstName = m.Sender?.FirstName, SenderLastName = m.Sender?.LastName
            }).ToList();
            var name1 = userNames.TryGetValue(u1, out var n1) ? $"{n1.FirstName} {n1.LastName}" : u1.ToString();
            var name2 = userNames.TryGetValue(u2, out var n2) ? $"{n2.FirstName} {n2.LastName}" : u2.ToString();
            return (dtos, nextCursor, hasMore, $"{name1} ↔ {name2}", "Dm");
        }

        if (parts[0].Equals("group", StringComparison.OrdinalIgnoreCase) && parts.Length >= 2 &&
            Guid.TryParse(parts[1], out var gId))
        {
            var grp = await _context.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == gId, cancellationToken);
            var query = _context.Messages.AsNoTracking().Where(m => m.GroupId == gId);
            if (beforeCursor.HasValue) query = query.Where(m => m.SentAt < beforeCursor.Value);
            var messages = await query
                .OrderByDescending(m => m.SentAt)
                .Take(limit + 1)
                .Include(m => m.Sender)
                .ToListAsync(cancellationToken);
            var hasMore = messages.Count > limit;
            var page = messages.Take(limit).OrderBy(m => m.SentAt).ToList();
            var nextCursor = hasMore ? page.Last().SentAt : (DateTime?)null;
            var dtos = page.Select(m => new AdminChatMessageDto
            {
                Id = m.Id, SenderId = m.SenderId, ReceiverId = null, GroupId = gId,
                Content = m.Content, AttachmentUrl = m.AttachmentUrl, AttachmentType = m.AttachmentType,
                SentAt = m.SentAt, IsEdited = m.IsEdited, DeletedAt = m.DeletedAt, IsFromMe = false,
                SenderUsername = m.Sender?.Username ?? "", SenderFirstName = m.Sender?.FirstName, SenderLastName = m.Sender?.LastName
            }).ToList();
            return (dtos, nextCursor, hasMore, grp?.Name ?? "Group", "Group");
        }

        return (Enumerable.Empty<AdminChatMessageDto>(), null, false, "", "");
    }

    public async Task<ExtendedMonitoringStatsDto> GetExtendedMonitoringStatsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var totalUsers = await _context.Users.CountAsync(cancellationToken);
        var totalMessages = await _context.Messages.CountAsync(cancellationToken);
        var totalGroups = await _context.Groups.CountAsync(cancellationToken);
        var disabledCount = await _context.Users.CountAsync(u => u.IsDisabled, cancellationToken);
        var suspendedCount = await _context.Users.CountAsync(u => u.SuspendedUntil.HasValue && u.SuspendedUntil > now, cancellationToken);
        var unreadCount = await _context.Messages.CountAsync(m => !m.IsRead && m.ReceiverId != null, cancellationToken);
        var new24h = await _context.Users.CountAsync(u => u.CreatedAt >= now.AddHours(-24), cancellationToken);
        var new7d = await _context.Users.CountAsync(u => u.CreatedAt >= now.AddDays(-7), cancellationToken);
        var new30d = await _context.Users.CountAsync(u => u.CreatedAt >= now.AddDays(-30), cancellationToken);
        var msg24h = await _context.Messages.CountAsync(m => m.SentAt >= now.AddHours(-24), cancellationToken);
        var msg7d = await _context.Messages.CountAsync(m => m.SentAt >= now.AddDays(-7), cancellationToken);
        var byRoleRaw = await _context.Users.AsNoTracking()
            .GroupBy(u => u.Role)
            .Select(g => new { Role = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var byRole = byRoleRaw
            .GroupBy(x => x.Role.ToRoleName(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Count), StringComparer.OrdinalIgnoreCase);
        return new ExtendedMonitoringStatsDto
        {
            TotalUsers = totalUsers, TotalMessages = totalMessages, TotalGroups = totalGroups,
            OnlineUsersCount = 0,
            DisabledUsersCount = disabledCount, SuspendedUsersCount = suspendedCount,
            UnreadMessagesCount = unreadCount,
            NewUsersLast24h = new24h, NewUsersLast7d = new7d, NewUsersLast30d = new30d,
            MessagesLast24h = msg24h, MessagesLast7d = msg7d,
            UsersByRole = byRole
        };
    }

    public async Task<IEnumerable<MessagesPerDayDto>> GetMessagesPerDayAsync(int days, CancellationToken cancellationToken = default)
    {
        var from = DateTime.UtcNow.Date.AddDays(-days);
        var data = await _context.Messages.AsNoTracking()
            .Where(m => m.SentAt >= from)
            .GroupBy(m => m.SentAt.Date)
            .Select(g => new MessagesPerDayDto { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);
        return data;
    }

    public async Task<(IEnumerable<MostActiveUserDto> Items, int TotalCount)> GetMostActiveUsersAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var totalCount = await _context.Messages.AsNoTracking()
            .Where(m => m.SenderId != Guid.Empty)
            .GroupBy(m => m.SenderId)
            .CountAsync(cancellationToken);
        var pageItems = await _context.Messages.AsNoTracking()
            .Where(m => m.SenderId != Guid.Empty)
            .GroupBy(m => m.SenderId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var userIds = pageItems.Select(x => x.UserId).ToList();
        var users = userIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _context.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Username, cancellationToken);
        var items = pageItems.Select(x => new MostActiveUserDto
        {
            UserId = x.UserId,
            Username = users.TryGetValue(x.UserId, out var un) ? un : "",
            MessageCount = x.Count
        }).ToList();
        return (items, totalCount);
    }

    public async Task<(IEnumerable<MostActiveGroupDto> Items, int TotalCount)> GetMostActiveGroupsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var totalCount = await _context.Messages.AsNoTracking()
            .Where(m => m.GroupId != null)
            .GroupBy(m => m.GroupId!.Value)
            .CountAsync(cancellationToken);
        var pageItems = await _context.Messages.AsNoTracking()
            .Where(m => m.GroupId != null)
            .GroupBy(m => m.GroupId!.Value)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var groupIds = pageItems.Select(x => x.GroupId).ToList();
        var groups = groupIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _context.Groups.AsNoTracking()
                .Where(g => groupIds.Contains(g.Id))
                .ToDictionaryAsync(g => g.Id, g => g.Name, cancellationToken);
        var items = pageItems.Select(x => new MostActiveGroupDto
        {
            GroupId = x.GroupId,
            GroupName = groups.TryGetValue(x.GroupId, out var name) ? name : "",
            MessageCount = x.Count
        }).ToList();
        return (items, totalCount);
    }

    public async Task<TableRowCountsDto> GetTableRowCountsAsync(CancellationToken cancellationToken = default)
    {
        var counts = new Dictionary<string, long>();
        counts["Users"] = await _context.Users.CountAsync(cancellationToken);
        counts["Messages"] = await _context.Messages.CountAsync(cancellationToken);
        counts["Groups"] = await _context.Groups.CountAsync(cancellationToken);
        counts["GroupMembers"] = await _context.GroupMembers.CountAsync(cancellationToken);
        counts["SavedMessages"] = await _context.SavedMessages.CountAsync(cancellationToken);
        counts["Contacts"] = await _context.Contacts.CountAsync(cancellationToken);
        counts["AdminAuditLogs"] = await _context.AdminAuditLogs.CountAsync(cancellationToken);
        return new TableRowCountsDto { Counts = counts };
    }

    public async Task<(IEnumerable<UserUsageDto> Items, int TotalCount)> GetUserUsageAsync(int page, int pageSize, string? sortBy, bool sortDesc, CancellationToken cancellationToken = default)
    {
        var orderCol = string.Equals(sortBy, "savedcount", StringComparison.OrdinalIgnoreCase) ? "SavedCount" : "MessageCount";
        var orderDir = sortDesc ? "DESC" : "ASC";
        var skip = (page - 1) * pageSize;

        var totalCount = await _context.Database
            .SqlQueryRaw<int>(
                $"""
                 WITH msg AS (SELECT "SenderId" AS "UserId" FROM "Messages" GROUP BY "SenderId"),
                 saved AS (SELECT "UserId" FROM "SavedMessages" GROUP BY "UserId"),
                 combined AS (SELECT "UserId" FROM msg UNION SELECT "UserId" FROM saved)
                 SELECT COUNT(*)::int FROM combined
                 """)
            .FirstOrDefaultAsync(cancellationToken);

        var orderByClause = $"\"{orderCol}\" {orderDir}"; // orderCol and orderDir are validated (MessageCount|SavedCount, DESC|ASC)
#pragma warning disable EF1002 // ORDER BY column and direction are validated; OFFSET/LIMIT are parameterized
        var pageRows = await _context.Database
            .SqlQueryRaw<UserUsageRow>(
                """
                WITH msg AS (SELECT "SenderId" AS "UserId", COUNT(*)::int AS "MessageCount" FROM "Messages" GROUP BY "SenderId"),
                saved AS (SELECT "UserId", COUNT(*)::int AS "SavedCount" FROM "SavedMessages" GROUP BY "UserId"),
                combined AS (
                  SELECT COALESCE(m."UserId", s."UserId") AS "UserId",
                         COALESCE(m."MessageCount", 0) AS "MessageCount",
                         COALESCE(s."SavedCount", 0) AS "SavedCount"
                  FROM msg m FULL OUTER JOIN saved s ON m."UserId" = s."UserId"
                )
                SELECT "UserId", "MessageCount", "SavedCount" FROM combined
                ORDER BY
                """ + orderByClause + """
                 OFFSET {0} LIMIT {1}
                """,
                skip,
                pageSize)
            .ToListAsync(cancellationToken);
#pragma warning restore EF1002

        var userIds = pageRows.Select(x => x.UserId).ToList();
        var users = userIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _context.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Username, cancellationToken);

        var items = pageRows.Select(r => new UserUsageDto
        {
            UserId = r.UserId,
            Username = users.TryGetValue(r.UserId, out var un) ? un : "",
            MessageCount = r.MessageCount,
            SavedMessagesCount = r.SavedCount
        }).ToList();
        return (items, totalCount);
    }

    private sealed record UserUsageRow(Guid UserId, int MessageCount, int SavedCount);

    public async Task<int> GetUnreadMessagesCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Messages.CountAsync(m => !m.IsRead && m.ReceiverId != null, cancellationToken);
    }

    public async Task<AdminChatDto?> GetChatByKeyAsync(string conversationKey, CancellationToken cancellationToken = default)
    {
        var parts = conversationKey.Split('_');
        if (parts.Length < 2) return null;
        if (parts[0].Equals("group", StringComparison.OrdinalIgnoreCase) && Guid.TryParse(parts[1], out var gId))
        {
            var g = await _context.Groups.AsNoTracking().FirstOrDefaultAsync(x => x.Id == gId, cancellationToken);
            if (g == null) return null;
            var last = await _context.Messages.AsNoTracking()
                .Where(m => m.GroupId == gId)
                .OrderByDescending(m => m.SentAt)
                .Select(m => new { m.Content, m.SentAt })
                .FirstOrDefaultAsync(cancellationToken);
            return new AdminChatDto { ConversationKey = conversationKey, Type = "Group", GroupId = gId, GroupName = g.Name, GroupProfilePictureUrl = g.ProfilePictureUrl, LastMessageContent = last?.Content, LastMessageAt = last?.SentAt };
        }
        if (parts[0].Equals("dm", StringComparison.OrdinalIgnoreCase) && parts.Length >= 3 && Guid.TryParse(parts[1], out var u1) && Guid.TryParse(parts[2], out var u2))
        {
            var last = await _context.Messages.AsNoTracking()
                .Where(m => m.GroupId == null && ((m.SenderId == u1 && m.ReceiverId == u2) || (m.SenderId == u2 && m.ReceiverId == u1)))
                .OrderByDescending(m => m.SentAt)
                .Select(m => new { m.Content, m.SentAt })
                .FirstOrDefaultAsync(cancellationToken);
            var users = await _context.Users.AsNoTracking()
                .Where(u => u.Id == u1 || u.Id == u2)
                .Select(u => new { u.Id, DisplayName = (u.FirstName + " " + u.LastName).Trim(), u.Username })
                .ToListAsync(cancellationToken);
            var name1 = users.FirstOrDefault(x => x.Id == u1);
            var name2 = users.FirstOrDefault(x => x.Id == u2);
            string? displayOrUsername(string? d, string? u) => string.IsNullOrWhiteSpace(d) ? u : d;
            return new AdminChatDto
            {
                ConversationKey = conversationKey,
                Type = "Dm",
                UserId1 = u1,
                UserId2 = u2,
                UserName1 = name1 != null ? displayOrUsername(name1.DisplayName, name1.Username) : null,
                UserName2 = name2 != null ? displayOrUsername(name2.DisplayName, name2.Username) : null,
                LastMessageContent = last?.Content,
                LastMessageAt = last?.SentAt
            };
        }
        return null;
    }
}
