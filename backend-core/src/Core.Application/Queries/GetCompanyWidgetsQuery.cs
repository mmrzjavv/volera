using MediatR;

namespace Core.Application.Queries;

public class GetCompanyWidgetsQuery : IRequest<IEnumerable<CompanyWidgetDto>>
{
    public Guid CompanyId { get; set; }
}

public class CompanyWidgetDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string WidgetId { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
