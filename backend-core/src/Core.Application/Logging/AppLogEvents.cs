using Microsoft.Extensions.Logging;

namespace Core.Application.Logging;

/// <summary>
/// Canonical application event names for structured logs (Seq / Loki / Datadog).
/// Prefer these EventIds so operators can query: EventId.Name = 'UserLoginSucceeded'
/// </summary>
public static class AppLogEvents
{
    // Auth / sessions (1xxx)
    public static readonly EventId UserRegistered = new(1001, nameof(UserRegistered));
    public static readonly EventId UserLoginSucceeded = new(1002, nameof(UserLoginSucceeded));
    public static readonly EventId UserLoginFailed = new(1003, nameof(UserLoginFailed));
    public static readonly EventId TokenRefreshed = new(1004, nameof(TokenRefreshed));
    public static readonly EventId TokenRefreshFailed = new(1005, nameof(TokenRefreshFailed));
    public static readonly EventId SupportLoginSucceeded = new(1006, nameof(SupportLoginSucceeded));
    public static readonly EventId SupportLoginFailed = new(1007, nameof(SupportLoginFailed));
    public static readonly EventId CompanyLoginSucceeded = new(1008, nameof(CompanyLoginSucceeded));
    public static readonly EventId CompanyLoginFailed = new(1009, nameof(CompanyLoginFailed));
    public static readonly EventId GuestSessionCreated = new(1010, nameof(GuestSessionCreated));
    public static readonly EventId CompanyRegistered = new(1011, nameof(CompanyRegistered));

    // Messaging (2xxx)
    public static readonly EventId MessageSent = new(2001, nameof(MessageSent));
    public static readonly EventId MessageEdited = new(2002, nameof(MessageEdited));
    public static readonly EventId MessageDeleted = new(2003, nameof(MessageDeleted));
    public static readonly EventId MessageForwarded = new(2004, nameof(MessageForwarded));
    public static readonly EventId MessagesMarkedRead = new(2005, nameof(MessagesMarkedRead));
    public static readonly EventId ChatRemoved = new(2006, nameof(ChatRemoved));

    // Groups / contacts (3xxx)
    public static readonly EventId GroupCreated = new(3001, nameof(GroupCreated));
    public static readonly EventId GroupMemberAdded = new(3002, nameof(GroupMemberAdded));
    public static readonly EventId GroupMemberRemoved = new(3003, nameof(GroupMemberRemoved));
    public static readonly EventId ContactAdded = new(3004, nameof(ContactAdded));

    // Calls (4xxx)
    public static readonly EventId CallInitiated = new(4001, nameof(CallInitiated));
    public static readonly EventId CallAccepted = new(4002, nameof(CallAccepted));
    public static readonly EventId CallRejected = new(4003, nameof(CallRejected));
    public static readonly EventId CallEnded = new(4004, nameof(CallEnded));
    public static readonly EventId CallNotifyFailed = new(4005, nameof(CallNotifyFailed));
    public static readonly EventId GroupCallInitiated = new(4006, nameof(GroupCallInitiated));

    // Media / push (5xxx)
    public static readonly EventId MediaUploaded = new(5001, nameof(MediaUploaded));
    public static readonly EventId MediaUploadFailed = new(5002, nameof(MediaUploadFailed));
    public static readonly EventId PushSent = new(5003, nameof(PushSent));
    public static readonly EventId PushFailed = new(5004, nameof(PushFailed));
    public static readonly EventId PushMisconfigured = new(5005, nameof(PushMisconfigured));

    // Admin / authz (6xxx)
    public static readonly EventId AdminActionSucceeded = new(6001, nameof(AdminActionSucceeded));
    public static readonly EventId AuthorizationDenied = new(6002, nameof(AuthorizationDenied));

    // Background jobs / integrations (7xxx)
    public static readonly EventId OutboxItemFailed = new(7001, nameof(OutboxItemFailed));
    public static readonly EventId OutboxProcessorFailed = new(7002, nameof(OutboxProcessorFailed));
    public static readonly EventId AiJobFailed = new(7003, nameof(AiJobFailed));
    public static readonly EventId AiIngestFailed = new(7004, nameof(AiIngestFailed));
    public static readonly EventId DatabaseInitFailed = new(7005, nameof(DatabaseInitFailed));

    // Pipeline / unhandled (9xxx)
    public static readonly EventId RequestFailed = new(9001, nameof(RequestFailed));
    public static readonly EventId UnhandledException = new(9002, nameof(UnhandledException));
}
