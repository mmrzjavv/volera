using Shared;

namespace Core.Domain.Entities;

public class AppSetting : BaseEntity
{
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;

    private AppSetting() { }

    public AppSetting(string key, string value)
    {
        Key = key;
        Value = value;
    }

    public void SetValue(string value)
    {
        Value = value;
        UpdatedAt = DateTime.UtcNow;
    }
}

public static class AppSettingKeys
{
    public const string AppVersion = "AppVersion";
    /// <summary>User id that receives all guest chat messages (inbox/support user).</summary>
    public const string GuestInboxUserId = "GuestInboxUserId";
}
