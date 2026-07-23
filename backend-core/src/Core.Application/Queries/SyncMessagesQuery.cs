using MediatR;
using Core.Application.DTOs;

namespace Core.Application.Queries;

public class SyncMessagesQuery : IRequest<SyncMessagesResultDto>
{
    public Guid CurrentUserId { get; set; }
    public Guid? PeerUserId { get; set; }
    public Guid? GroupId { get; set; }
    public DateTime? AfterSentAt { get; set; }
    public Guid? AfterId { get; set; }
    public int Limit { get; set; } = 50;
}
