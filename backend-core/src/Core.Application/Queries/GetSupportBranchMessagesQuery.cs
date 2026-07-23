using MediatR;
using Core.Application.DTOs;

namespace Core.Application.Queries;

public class GetSupportBranchMessagesQuery : IRequest<IEnumerable<BranchMessageDto>>
{
    public Guid SupportUserId { get; set; }
    public Guid BranchId { get; set; }
    public int Limit { get; set; } = 50;
    public DateTime? Before { get; set; }
}
