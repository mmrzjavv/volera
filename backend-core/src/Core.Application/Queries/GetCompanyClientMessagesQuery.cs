using MediatR;
using Core.Domain.Entities;

namespace Core.Application.Queries;

public class GetCompanyClientMessagesQuery : IRequest<IEnumerable<Message>?>
{
    /// <summary>Validated from client token; null if token invalid.</summary>
    public Guid? ClientUserId { get; set; }
    public Guid? BranchId { get; set; }
    public int Limit { get; set; } = 50;
    public DateTime? Before { get; set; }
}
