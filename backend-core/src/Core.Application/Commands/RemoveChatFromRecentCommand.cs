using MediatR;

namespace Core.Application.Commands;

/// <summary>
/// Removes a chat from the user's recent chats list.
/// For direct chats: hides the chat (adds to HiddenChat).
/// For group chats: leaves the group.
/// </summary>
public class RemoveChatFromRecentCommand : IRequest
{
    public Guid CurrentUserId { get; set; }
    public Guid? OtherUserId { get; set; }
    public Guid? GroupId { get; set; }
}
