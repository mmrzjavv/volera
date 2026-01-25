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

    public async Task<Call?> GetActiveCallByUserIdAsync(Guid userId)
    {
        return await _context.Calls
            .FirstOrDefaultAsync(c => (c.CallerId == userId || c.ReceiverId == userId) && c.Status == CallStatus.Ringing || c.Status == CallStatus.Connected);
    }
}