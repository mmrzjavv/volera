namespace Core.Application.Interfaces;

public interface IOnlineUserService
{
    Task<bool> IsUserOnline(Guid userId);
    Task<IEnumerable<Guid>> GetOnlineUserIds();
    Task UserConnected(Guid userId);
    Task UserDisconnected(Guid userId);
}