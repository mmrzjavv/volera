using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ContactRepository : Repository<Contact>, IContactRepository
{
    public ContactRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Contact>> GetContactsByUserIdAsync(Guid userId)
    {
        return await _context.Contacts
            .Include(c => c.ContactUser)
            .Where(c => c.OwnerUserId == userId)
            .ToListAsync();
    }

    public async Task<Contact?> GetContactAsync(Guid ownerUserId, Guid contactUserId)
    {
        return await _context.Contacts
            .FirstOrDefaultAsync(c => c.OwnerUserId == ownerUserId && c.ContactUserId == contactUserId);
    }

    public async Task<Contact?> GetContactByPhoneNumberAsync(Guid ownerUserId, string phoneNumber)
    {
        return await _context.Contacts
            .FirstOrDefaultAsync(c => c.OwnerUserId == ownerUserId && c.ContactPhoneNumber == phoneNumber);
    }

    public async Task<bool> ContactExistsAsync(Guid ownerUserId, Guid contactUserId)
    {
        return await _context.Contacts
            .AnyAsync(c => c.OwnerUserId == ownerUserId && c.ContactUserId == contactUserId);
    }

    public async Task<bool> ContactExistsAsync(Guid ownerUserId, string phoneNumber)
    {
        return await _context.Contacts
             .AnyAsync(c => c.OwnerUserId == ownerUserId && c.ContactPhoneNumber == phoneNumber);
    }
}
