using Core.Domain.Entities;

namespace Core.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByPhoneNumberAsync(string phoneNumber);
    Task<bool> IsUsernameUniqueAsync(string username);
    Task<bool> IsPhoneNumberUniqueAsync(string phoneNumber);
}