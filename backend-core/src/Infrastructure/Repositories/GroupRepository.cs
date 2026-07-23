using Core.Domain.Entities;
using Core.Domain.Enums;
using Core.Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class GroupRepository : Repository<Group>, IGroupRepository
{
    public GroupRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Group>> GetGroupsForUserAsync(Guid userId)
    {
        return await _dbSet
            .Include(g => g.Members)
            .Where(g => g.Members.Any(m => m.UserId == userId))
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Group>> GetChannelsForUserAsync(Guid userId)
    {
        return await _dbSet
            .Include(g => g.Members)
            .Where(g => g.Kind == GroupKind.Channel && g.Members.Any(m => m.UserId == userId))
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Group>> GetRegularGroupsForUserAsync(Guid userId)
    {
        return await _dbSet
            .Include(g => g.Members)
            .Where(g => g.Kind == GroupKind.Group && g.Members.Any(m => m.UserId == userId))
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    public async Task<Group?> GetGroupWithMembersAsync(Guid groupId)
    {
        return await _dbSet
            .Include(g => g.Members)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(g => g.Id == groupId);
    }

    public async Task<Group?> GetByInviteCodeAsync(string inviteCode)
    {
        return await _dbSet
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.InviteCode == inviteCode);
    }

    public async Task<Group?> GetByPublicUsernameAsync(string publicUsername)
    {
        var normalized = publicUsername.Trim().TrimStart('@').ToLowerInvariant();
        return await _dbSet
            .Include(g => g.Members)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(g => g.Kind == GroupKind.Channel && g.PublicUsername == normalized);
    }

    public async Task<IEnumerable<Group>> SearchPublicChannelsAsync(string query, int limit = 20)
    {
        limit = Math.Clamp(limit, 1, 50);
        var q = query.Trim().TrimStart('@').ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(q))
            return Enumerable.Empty<Group>();

        return await _dbSet
            .AsNoTracking()
            .Where(g => g.Kind == GroupKind.Channel && g.IsPublic &&
                        (g.PublicUsername!.Contains(q) || g.Name.ToLower().Contains(q)))
            .OrderBy(g => g.Name)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<int> GetSubscriberCountAsync(Guid channelId, CancellationToken cancellationToken = default)
    {
        return await _context.GroupMembers.CountAsync(m => m.GroupId == channelId, cancellationToken);
    }

    public async Task<IEnumerable<Group>> GetGroupsByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
            return Enumerable.Empty<Group>();

        return await _dbSet
            .AsNoTracking()
            .Where(g => idList.Contains(g.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsPublicUsernameTakenAsync(string publicUsername, Guid? excludeChannelId = null, CancellationToken cancellationToken = default)
    {
        var normalized = publicUsername.Trim().TrimStart('@').ToLowerInvariant();
        var query = _dbSet.Where(g => g.PublicUsername == normalized);
        if (excludeChannelId.HasValue)
            query = query.Where(g => g.Id != excludeChannelId.Value);
        return await query.AnyAsync(cancellationToken);
    }

    public async Task<Group?> GetChannelByLinkedDiscussionGroupIdAsync(Guid discussionGroupId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Kind == GroupKind.Channel && g.LinkedDiscussionGroupId == discussionGroupId, cancellationToken);
    }

    public void AddMember(GroupMember member)
    {
        _context.GroupMembers.Add(member);
    }
}
