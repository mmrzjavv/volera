using MediatR;

namespace Core.Application.Commands;

public class SetupCompanyAiWidgetCommand : IRequest<SetupCompanyAiWidgetResult>
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
}

public class SetupCompanyAiWidgetResult
{
    public Guid AiWidgetId { get; init; }
    public string TenantId { get; init; } = string.Empty;
}
