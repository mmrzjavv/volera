using Core.Application.DTOs;

namespace Core.Application.Interfaces;

public interface IStoryNotificationService
{
    Task NotifyStoryCreated(Guid authorUserId, Guid storyId, IEnumerable<Guid> contactUserIds);
    Task NotifyStoryDeleted(Guid authorUserId, Guid storyId, IEnumerable<Guid> contactUserIds);
}
