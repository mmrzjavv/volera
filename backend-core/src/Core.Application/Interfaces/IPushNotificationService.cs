namespace Core.Application.Interfaces;

public interface IPushNotificationService
{
    Task SendPushNotificationAsync(Guid userId, string title, string body, object? data = null);
    Task<string> GetVapidPublicKeyAsync();
    Task AddSubscriptionAsync(Guid userId, string endpoint, string p256dh, string auth);
    Task RemoveAllSubscriptionsAsync(Guid userId);
}
