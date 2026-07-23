namespace Core.Application.Interfaces;

/// <summary>
/// Short-lived session token for AI widget visitors. Token maps to branchId for RAG scope.
/// </summary>
public interface IAiWidgetSessionService
{
    Task<string> CreateSessionAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<Guid?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
}
