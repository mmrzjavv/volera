using Core.Domain.Entities;

namespace Core.Domain.Interfaces;

public interface ICallRepository : IRepository<Call>
{
    Task<IEnumerable<Call>> GetCallsByUserIdAsync(Guid userId);
    Task<(IEnumerable<Call> Items, int TotalCount)> GetCallsByUserIdAsync(
        Guid userId, 
        int page, 
        int pageSize, 
        string? term, 
        DateTime? dateFrom, 
        DateTime? dateTo, 
        string? sortBy, 
        bool sortDescending);
    Task<Call?> GetActiveCallByUserIdAsync(Guid userId);
}