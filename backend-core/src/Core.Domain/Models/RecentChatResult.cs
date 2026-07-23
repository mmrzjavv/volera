using System;
using Core.Domain.Entities;

namespace Core.Domain.Models;

public class RecentChatResult
{
    public Guid? OtherUserId { get; set; }
    public Guid? GroupId { get; set; }
    public Message? LastMessage { get; set; }
    public int UnreadCount { get; set; }
}
