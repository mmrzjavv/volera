using System.Threading.Tasks;

namespace Core.Application.Interfaces;

public interface IMessageNotificationService
{
    Task SendMessage(Guid messageId, Guid senderId, Guid? receiverId, Guid? groupId, string content, DateTime sentAt, string? attachmentUrl, string? attachmentType, Guid? branchId = null, Guid? replyToMessageId = null, Guid? supportSenderId = null);
    Task NotifyMessageEdited(Guid messageId, Guid senderId, Guid? receiverId, Guid? groupId, string newContent, DateTime editedAt);
    Task NotifyMessageDeleted(Guid messageId, Guid senderId, Guid? receiverId, Guid? groupId, DateTime deletedAt);
    Task NotifyMessagesRead(Guid userId, Guid senderId);
    Task NotifyMessageReactionsUpdated(Guid messageId);
    Task NotifyMessagePinnedUpdated(Guid messageId);
}
