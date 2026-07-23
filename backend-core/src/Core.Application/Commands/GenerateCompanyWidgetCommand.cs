using MediatR;

namespace Core.Application.Commands;

public class GenerateCompanyWidgetCommand : IRequest<GenerateCompanyWidgetResult>
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
}

public class GenerateCompanyWidgetResult
{
    public Guid WidgetEntityId { get; init; }
    public string WidgetId { get; init; } = string.Empty;
    public string? WidgetToken { get; init; }
}
