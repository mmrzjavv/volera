using Core.Domain.Entities;

namespace Core.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByPhoneNumberAsync(string phoneNumber);
    Task<(IEnumerable<User> Items, int TotalCount)> GetUsersAsync(int page, int pageSize, string? term, Guid? excludeUserId);
    Task<bool> IsUsernameUniqueAsync(string username);
    Task<bool> IsPhoneNumberUniqueAsync(string phoneNumber);

    /// <summary>
    /// Returns users for the given ids using a single batched query.
    /// </summary>
    Task<IEnumerable<User>> GetUsersByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the count of users with the SuperAdmin role.
    /// </summary>
    Task<int> CountSuperAdminsAsync(CancellationToken cancellationToken = default);
}