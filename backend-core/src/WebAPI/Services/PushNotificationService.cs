using Core.Application.Interfaces;
using Core.Application.Logging;
using Core.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WebPush;

namespace WebAPI.Services;

public class PushNotificationService : IPushNotificationService
{
    private readonly IConfiguration _configuration;
    private readonly IPushSubscriptionRepository _subscriptionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PushNotificationService> _logger;
    private string? _vapidPublicKey;
    private string? _vapidPrivateKey;
    private readonly bool _vapidConfigured;

    public PushNotificationService(
        IConfiguration configuration,
        IPushSubscriptionRepository subscriptionRepository,
        IUnitOfWork unitOfWork,
        ILogger<PushNotificationService> logger)
    {
        _configuration = configuration;
        _subscriptionRepository = subscriptionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _vapidConfigured = LoadVapidKeys();
    }

    private static int _vapidWarningEmitted;

    private bool LoadVapidKeys()
    {
        _vapidPublicKey = _configuration["PushNotifications:VapidPublicKey"] ?? _configuration["VapidPublicKey"];
        _vapidPrivateKey = _configuration["PushNotifications:VapidPrivateKey"] ?? _configuration["VapidPrivateKey"];

        if (!string.IsNullOrEmpty(_vapidPublicKey) && !string.IsNullOrEmpty(_vapidPrivateKey))
            return true;

        if (Interlocked.Exchange(ref _vapidWarningEmitted, 1) == 0)
        {
            AppLog.Warning(_logger, AppLogEvents.PushMisconfigured,
                "Reason: VapidKeysMissing | Result: UsingEphemeralDevKeys");
        }

        var keys = VapidHelper.GenerateVapidKeys();
        _vapidPublicKey = keys.PublicKey;
        _vapidPrivateKey = keys.PrivateKey;
        return false;
    }

    public Task<string> GetVapidPublicKeyAsync()
    {
        return Task.FromResult(_vapidPublicKey ?? string.Empty);
    }

    public async Task SendPushNotificationAsync(Guid userId, string title, string body, object? data = null)
    {
        var userSubscriptions = await _subscriptionRepository.GetByUserIdAsync(userId);
        if (!userSubscriptions.Any())
            return;

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
                AppLog.Warning(_logger, AppLogEvents.PushFailed, ex,
                    "UserId: {UserId} | StatusCode: {StatusCode} | Error: {ErrorType} | Result: Failure",
                    userId, ex.StatusCode, ex.GetType().Name);

                if (ex.StatusCode == System.Net.HttpStatusCode.Gone ||
                    ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await RemoveSubscriptionAsync(userId, subscription.Endpoint);
                }
                return 0;
            }
            catch (Exception ex)
            {
                AppLog.Error(_logger, AppLogEvents.PushFailed, ex,
                    "UserId: {UserId} | Error: {ErrorType} | Result: Failure",
                    userId, ex.GetType().Name);
                return 0;
            }
        });

        var results = await Task.WhenAll(tasks);
        var sentCount = results.Sum();
        if (sentCount > 0)
        {
            AppLog.Info(_logger, AppLogEvents.PushSent,
                "UserId: {UserId} | DeliveredCount: {DeliveredCount} | VapidConfigured: {VapidConfigured} | Result: Success",
                userId, sentCount, _vapidConfigured);
        }
    }

    public async Task AddSubscriptionAsync(Guid userId, string endpoint, string p256dh, string auth)
    {
        var existingSubscription = await _subscriptionRepository.GetByEndpointAsync(endpoint);
        if (existingSubscription != null)
        {
            if (existingSubscription.UserId != userId)
            {
                _subscriptionRepository.Delete(existingSubscription);
                await _unitOfWork.SaveChangesAsync();

                var subscription = new Core.Domain.Entities.PushSubscription(userId, endpoint, p256dh, auth);
                await _subscriptionRepository.AddAsync(subscription);
            }
            else
            {
                existingSubscription.UpdateKeys(p256dh, auth);
                existingSubscription.UpdatedAt = DateTime.UtcNow;
            }
        }
        else
        {
            var subscription = new Core.Domain.Entities.PushSubscription(userId, endpoint, p256dh, auth);
            await _subscriptionRepository.AddAsync(subscription);
        }

        await _unitOfWork.SaveChangesAsync();
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
