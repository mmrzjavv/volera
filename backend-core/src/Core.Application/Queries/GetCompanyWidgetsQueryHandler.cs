using MediatR;
using Core.Application.Queries;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class GetCompanyWidgetsQueryHandler : IRequestHandler<GetCompanyWidgetsQuery, IEnumerable<CompanyWidgetDto>>
{
    private readonly ICompanyWidgetRepository _widgetRepository;

    public GetCompanyWidgetsQueryHandler(ICompanyWidgetRepository widgetRepository)
    {
        _widgetRepository = widgetRepository;
    }

    public async Task<IEnumerable<CompanyWidgetDto>> Handle(GetCompanyWidgetsQuery request, CancellationToken cancellationToken)
    {
        var widgets = await _widgetRepository.GetByCompanyIdAsync(request.CompanyId, cancellationToken);
        return widgets.Select(w => new CompanyWidgetDto
        {
            Id = w.Id,
            CompanyId = w.CompanyId,
            BranchId = w.BranchId,
            WidgetId = w.WidgetId,
            IsActive = w.IsActive
        });
    }
}
