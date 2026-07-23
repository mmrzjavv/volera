using Core.Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using WebPush;

namespace WebAPI.Services;

public class PushNotificationService : IPushNotificationService
{
    private readonly IConfiguration _configuration;
    private readonly IPushSubscriptionRepository _subscriptionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private string? _vapidPublicKey;
    private string? _vapidPrivateKey;

    public PushNotificationService(
        IConfiguration configuration,
        IPushSubscriptionRepository subscriptionRepository,
        IUnitOfWork unitOfWork)
    {
        _configuration = configuration;
        _subscriptionRepository = subscriptionRepository;
        _unitOfWork = unitOfWork;
        LoadVapidKeys();
    }

    private void LoadVapidKeys()
    {
        _vapidPublicKey = _configuration["PushNotifications:VapidPublicKey"] ?? _configuration["VapidPublicKey"];
        _vapidPrivateKey = _configuration["PushNotifications:VapidPrivateKey"] ?? _configuration["VapidPrivateKey"];

        // If keys are not configured, generate them (for development only)
        if (string.IsNullOrEmpty(_vapidPublicKey) || string.IsNullOrEmpty(_vapidPrivateKey))
        {
            Console.WriteLine("[PushNotificationService] WARNING: VAPID keys not configured. Generating temporary keys for development.");
            Console.WriteLine("[PushNotificationService] For production, configure VAPID keys in appsettings.json");

            // Generate temporary keys using WebPush library
            // Note: In production, you should generate these once and store them securely
            var keys = VapidHelper.GenerateVapidKeys();
            _vapidPublicKey = keys.PublicKey;
            _vapidPrivateKey = keys.PrivateKey;

            Console.WriteLine($"[PushNotificationService] Generated VAPID Public Key: {_vapidPublicKey}");
            Console.WriteLine($"[PushNotificationService] Add this to appsettings.json: \"VapidPublicKey\": \"{_vapidPublicKey}\"");
            Console.WriteLine($"[PushNotificationService] Add this to appsettings.json: \"VapidPrivateKey\": \"{_vapidPrivateKey}\"");
        }
    }

    public Task<string> GetVapidPublicKeyAsync()
    {
        return Task.FromResult(_vapidPublicKey ?? string.Empty);
    }

    public async Task SendPushNotificationAsync(Guid userId, string title, string body, object? data = null)
    {
        var userSubscriptions = await _subscriptionRepository.GetByUserIdAsync(userId);
        if (!userSubscriptions.Any())
        {
            Console.WriteLine($"[PushNotificationService] No push subscriptions found for user {userId}");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            title,
            body,
            data = data ?? new { }
        });

        var tasks = userSubscriptions.Select(async subscription =>
        {
            try
            {
                var pushSubscription = new WebPush.PushSubscription(
                    subscription.Endpoint,
                    subscription.P256dh,
                    subscription.Auth
                );

                var vapidDetails = new VapidDetails(
                    subject: _configuration["PushNotifications:Subject"] ?? "mailto:admin@voicecallapp.com",
                    publicKey: _vapidPublicKey!,
                    privateKey: _vapidPrivateKey!
                );

                var webPushClient = new WebPushClient();
                await webPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails);
                return 1;
            }
            catch (WebPushException ex)
            {
                Console.WriteLine($"[PushNotificationService] Error sending push to {userId}: {ex.Message}");

                // Remove invalid subscriptions
                if (ex.StatusCode == System.Net.HttpStatusCode.Gone ||
                    ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await RemoveSubscriptionAsync(userId, subscription.Endpoint);
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PushNotificationService] Unexpected error for user {userId}: {ex.Message}");
                return 0;
            }
        });

        var results = await Task.WhenAll(tasks);
        var sentCount = results.Sum();
        Console.WriteLine($"[PushNotificationService] Push notification sent to user {userId} ({sentCount} subscription(s))");
    }

    public async Task AddSubscriptionAsync(Guid userId, string endpoint, string p256dh, string auth)
    {
        // Check if subscription already exists
        var existingSubscription = await _subscriptionRepository.GetByEndpointAsync(endpoint);
        if (existingSubscription != null)
        {
            // Update existing subscription if needed
            if (existingSubscription.UserId != userId)
            {
                // If the endpoint exists but belongs to a different user, 
                // we should remove the old association and create a new one.
                _subscriptionRepository.Delete(existingSubscription);
                await _unitOfWork.SaveChangesAsync();

                var subscription = new Core.Domain.Entities.PushSubscription(userId, endpoint, p256dh, auth);
                await _subscriptionRepository.AddAsync(subscription);
            }
            else
            {
                // Same user, same endpoint. Just update keys/timestamp.
                existingSubscription.UpdateKeys(p256dh, auth);
                existingSubscription.UpdatedAt = DateTime.UtcNow;
            }
        }
        else
        {
            // Create new subscription
            var subscription = new Core.Domain.Entities.PushSubscription(userId, endpoint, p256dh, auth);
            await _subscriptionRepository.AddAsync(subscription);
        }

        await _unitOfWork.SaveChangesAsync();
        Console.WriteLine($"[PushNotificationService] Added/Updated push subscription for user {userId}");
    }

    public async Task RemoveAllSubscriptionsAsync(Guid userId)
    {
        await _subscriptionRepository.DeleteByUserIdAsync(userId);
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task RemoveSubscriptionAsync(Guid userId, string endpoint)
    {
        await _subscriptionRepository.DeleteByEndpointAsync(userId, endpoint);
        await _unitOfWork.SaveChangesAsync();
    }
}
