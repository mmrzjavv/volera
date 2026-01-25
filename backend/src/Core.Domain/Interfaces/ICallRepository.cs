using Core.Domain.Entities;

namespace Core.Domain.Interfaces;

public interface ICallRepository : IRepository<Call>
{
    Task<IEnumerable<Call>> GetCallsByUserIdAsync(Guid userId);
    Task<Call?> GetActiveCallByUserIdAsync(Guid userId);
}