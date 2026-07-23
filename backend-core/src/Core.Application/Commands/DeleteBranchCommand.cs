using MediatR;

namespace Core.Application.Commands;

public class DeleteBranchCommand : IRequest
{
    public Guid BranchId { get; set; }
    public Guid CompanyId { get; set; }
}
