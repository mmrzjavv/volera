using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class GroupCallRepository : Repository<GroupCall>, IGroupCallRepository
{
    public GroupCallRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<GroupCall?> GetByIdWithParticipantsAsync(Guid id)
    {
        return await _context.GroupCalls
            .Include(gc => gc.Participants)
            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(gc => gc.Id == id);
    }

    public async Task<GroupCall?> GetActiveByGroupIdAsync(Guid groupId)
    {
        return await _context.GroupCalls
            .Include(gc => gc.Participants)
            .Where(gc => gc.GroupId == groupId && (gc.Status == GroupCallStatus.Ringing || gc.Status == GroupCallStatus.Active))
            .OrderByDescending(gc => gc.StartTime)
            .FirstOrDefaultAsync();
    }

    public async Task<(IEnumerable<GroupCall> Items, int TotalCount)> GetHistoryByGroupIdAsync(Guid groupId, int page, int pageSize)
    {
        var query = _context.GroupCalls
            .Include(gc => gc.Participants)
            .ThenInclude(p => p.User)
            .Where(gc => gc.GroupId == groupId && gc.Status == GroupCallStatus.Ended)
            .OrderByDescending(gc => gc.StartTime);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}

