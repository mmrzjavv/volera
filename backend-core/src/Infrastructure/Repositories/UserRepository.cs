using System.Threading.Tasks;
using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context) { }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User?> GetByPhoneNumberAsync(string phoneNumber)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
    }

    public async Task<(IEnumerable<User> Items, int TotalCount)> GetUsersAsync(int page, int pageSize, string? term, Guid? excludeUserId)
    {
        var query = _context.Users.AsQueryable();

        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(term))
        {
            term = term.ToLower();
            query = query.Where(u =>
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term) ||
                u.Username.ToLower().Contains(term) ||
                u.PhoneNumber.Contains(term));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<bool> IsUsernameUniqueAsync(string username)
    {
        return !await _context.Users.AnyAsync(u => u.Username == username);
    }

    public async Task<bool> IsPhoneNumberUniqueAsync(string phoneNumber)
    {
        return !await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber);
    }

    public async Task<IEnumerable<User>> GetUsersByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
        {
            return Enumerable.Empty<User>();
        }

        return await _context.Users
            .AsNoTracking()
            .Where(u => idList.Contains(u.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountSuperAdminsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .CountAsync(u => u.Role == UserRole.SuperAdmin, cancellationToken);
    }
}