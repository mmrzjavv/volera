using MediatR;

namespace Core.Application.Commands;

public class DeleteCompanyWidgetCommand : IRequest
{
    public Guid WidgetId { get; set; }
    public Guid CompanyId { get; set; }
}
