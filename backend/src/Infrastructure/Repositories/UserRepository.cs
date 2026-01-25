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

    public async Task<bool> IsUsernameUniqueAsync(string username)
    {
        return !await _context.Users.AnyAsync(u => u.Username == username);
    }

    public async Task<bool> IsPhoneNumberUniqueAsync(string phoneNumber)
    {
        return !await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber);
    }
}