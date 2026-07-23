using MediatR;

namespace Core.Application.Commands;

public class DeleteSupportUserCommand : IRequest
{
    public Guid SupportUserId { get; set; }
    public Guid CompanyId { get; set; }
}
