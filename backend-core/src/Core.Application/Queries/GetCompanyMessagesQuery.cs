using MediatR;
using Core.Domain.Entities;

namespace Core.Application.Queries;

public class GetCompanyMessagesQuery : IRequest<IEnumerable<Message>>
{
    public Guid SupportUserId { get; set; }
    public Guid BranchId { get; set; }
    public int Limit { get; set; } = 50;
    public DateTime? Before { get; set; }
}
