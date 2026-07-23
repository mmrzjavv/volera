using MediatR;

namespace Core.Application.Commands;

public class UnassignSupportUserFromBranchCommand : IRequest
{
    public Guid SupportUserId { get; set; }
    public Guid BranchId { get; set; }
    public Guid CompanyId { get; set; }
}
