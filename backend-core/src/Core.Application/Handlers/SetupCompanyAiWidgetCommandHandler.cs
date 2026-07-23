using MediatR;
using Core.Application.Commands;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class SetupCompanyAiWidgetCommandHandler : IRequestHandler<SetupCompanyAiWidgetCommand, SetupCompanyAiWidgetResult>
{
    private readonly IBranchRepository _branchRepository;
    private readonly ICompanyAiWidgetRepository _aiWidgetRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetupCompanyAiWidgetCommandHandler(
        IBranchRepository branchRepository,
        ICompanyAiWidgetRepository aiWidgetRepository,
        IUnitOfWork unitOfWork)
    {
        _branchRepository = branchRepository;
        _aiWidgetRepository = aiWidgetRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SetupCompanyAiWidgetResult> Handle(SetupCompanyAiWidgetCommand request, CancellationToken cancellationToken)
    {
        var branch = await _branchRepository.GetByIdAsync(request.BranchId);
        if (branch == null || branch.CompanyId != request.CompanyId)
            throw new InvalidOperationException("Branch not found or does not belong to company.");

        var existing = await _aiWidgetRepository.GetByBranchIdAsync(request.BranchId, cancellationToken);
        if (existing != null)
        {
            return new SetupCompanyAiWidgetResult
            {
                AiWidgetId = existing.Id,
                TenantId = existing.TenantId
            };
        }

        var tenantId = $"{request.CompanyId}_{request.BranchId}";
        var widget = new CompanyAiWidget(request.CompanyId, request.BranchId, tenantId);
        await _aiWidgetRepository.AddAsync(widget);
        await _unitOfWork.SaveChangesAsync();

        return new SetupCompanyAiWidgetResult
        {
            AiWidgetId = widget.Id,
            TenantId = widget.TenantId
        };
    }
}
