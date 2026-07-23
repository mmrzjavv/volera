using MediatR;
using Core.Application.Queries;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class GetWidgetIdByBranchQueryHandler : IRequestHandler<GetWidgetIdByBranchQuery, string?>
{
    private readonly ICompanyWidgetRepository _widgetRepository;

    public GetWidgetIdByBranchQueryHandler(ICompanyWidgetRepository widgetRepository)
    {
        _widgetRepository = widgetRepository;
    }

    public async Task<string?> Handle(GetWidgetIdByBranchQuery request, CancellationToken cancellationToken)
    {
        var widgets = await _widgetRepository.GetByBranchIdAsync(request.BranchId, cancellationToken);
        var active = widgets.FirstOrDefault(w => w.IsActive);
        return active?.WidgetId;
    }
}
