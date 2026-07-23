using Shared;

namespace Core.Domain.Entities;

public class PushSubscription : BaseEntity
{
    public Guid UserId { get; private set; }
    public User User { get; private set; }
    public string Endpoint { get; private set; }
    public string P256dh { get; private set; }
    public string Auth { get; private set; }

    private PushSubscription() { } // EF Core

    public PushSubscription(Guid userId, string endpoint, string p256dh, string auth)
    {
        UserId = userId;
        Endpoint = endpoint;
        P256dh = p256dh;
        Auth = auth;
    }

    public void UpdateKeys(string p256dh, string auth)
    {
        P256dh = p256dh;
        Auth = auth;
    }
}
