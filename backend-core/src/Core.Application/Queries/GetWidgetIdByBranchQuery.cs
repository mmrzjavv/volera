using MediatR;

namespace Core.Application.Queries;

public class GetWidgetIdByBranchQuery : IRequest<string?>
{
    public Guid BranchId { get; set; }
}
