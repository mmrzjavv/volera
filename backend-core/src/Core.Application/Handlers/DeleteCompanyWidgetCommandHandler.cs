using MediatR;
using Core.Application.Commands;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class DeleteCompanyWidgetCommandHandler : IRequestHandler<DeleteCompanyWidgetCommand>
{
    private readonly ICompanyWidgetRepository _widgetRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCompanyWidgetCommandHandler(ICompanyWidgetRepository widgetRepository, IUnitOfWork unitOfWork)
    {
        _widgetRepository = widgetRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteCompanyWidgetCommand request, CancellationToken cancellationToken)
    {
        var widget = await _widgetRepository.GetByIdAsync(request.WidgetId);
        if (widget == null || widget.CompanyId != request.CompanyId)
            throw new InvalidOperationException("Widget not found.");

        _widgetRepository.Delete(widget);
        await _unitOfWork.SaveChangesAsync();
    }
}
