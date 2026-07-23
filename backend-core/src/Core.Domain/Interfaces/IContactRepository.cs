using Core.Domain.Entities;

namespace Core.Domain.Interfaces;

public interface IContactRepository : IRepository<Contact>
{
    Task<IEnumerable<Contact>> GetContactsByUserIdAsync(Guid userId);
    Task<Contact?> GetContactAsync(Guid ownerUserId, Guid contactUserId);
    Task<Contact?> GetContactByPhoneNumberAsync(Guid ownerUserId, string phoneNumber);
    Task<bool> ContactExistsAsync(Guid ownerUserId, Guid contactUserId);
    Task<bool> ContactExistsAsync(Guid ownerUserId, string phoneNumber);
}
