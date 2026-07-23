using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CallRepository : Repository<Call>, ICallRepository
{
    public CallRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Call>> GetCallsByUserIdAsync(Guid userId)
    {
        return await _context.Calls
            .Include(c => c.Caller)
            .Include(c => c.Receiver)
            .Where(c => c.CallerId == userId || c.ReceiverId == userId)
            .OrderByDescending(c => c.StartTime)
            .ToListAsync();
    }

    public async Task<(IEnumerable<Call> Items, int TotalCount)> GetCallsByUserIdAsync(
        Guid userId, 
        int page, 
        int pageSize, 
        string? term, 
        DateTime? dateFrom, 
        DateTime? dateTo, 
        string? sortBy, 
        bool sortDescending)
    {
        var query = _context.Calls
            .Include(c => c.Caller)
            .Include(c => c.Receiver)
            .Where(c => c.CallerId == userId || c.ReceiverId == userId);

        // Filtering
        if (dateFrom.HasValue)
            query = query.Where(c => c.StartTime >= dateFrom.Value);
        
        if (dateTo.HasValue)
            query = query.Where(c => c.StartTime <= dateTo.Value);

        // Sorting
        if (!string.IsNullOrEmpty(sortBy))
        {
            switch (sortBy.ToLower())
            {
                case "duration":
                    query = sortDescending ? query.OrderByDescending(c => c.Duration) : query.OrderBy(c => c.Duration);
                    break;
                case "starttime":
                default:
                    query = sortDescending ? query.OrderByDescending(c => c.StartTime) : query.OrderBy(c => c.StartTime);
                    break;
            }
        }
        else
        {
            query = query.OrderByDescending(c => c.StartTime);
        }

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return (items, totalCount);
    }

    public async Task<Call?> GetActiveCallByUserIdAsync(Guid userId)
    {
        return await _context.Calls
            .FirstOrDefaultAsync(c => (c.CallerId == userId || c.ReceiverId == userId) && (c.Status == CallStatus.Ringing || c.Status == CallStatus.Connected));
    }
}