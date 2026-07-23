using MediatR;

namespace Core.Application.Queries;

public class GetTotalMessagesCountQuery : IRequest<int>
{
    public Guid UserId { get; set; }
}
