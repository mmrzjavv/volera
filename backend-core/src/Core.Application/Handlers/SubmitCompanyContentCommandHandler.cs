using MediatR;
using Core.Application.Commands;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class SubmitCompanyContentCommandHandler : IRequestHandler<SubmitCompanyContentCommand, SubmitCompanyContentResult>
{
    private readonly IBranchRepository _branchRepository;
    private readonly ICompanyAiWidgetRepository _aiWidgetRepository;
    private readonly IAiContentBlockRepository _contentBlockRepository;
    private readonly IAiJobEnqueuer _jobEnqueuer;
    private readonly IUnitOfWork _unitOfWork;

    public SubmitCompanyContentCommandHandler(
        IBranchRepository branchRepository,
        ICompanyAiWidgetRepository aiWidgetRepository,
        IAiContentBlockRepository contentBlockRepository,
        IAiJobEnqueuer jobEnqueuer,
        IUnitOfWork unitOfWork)
    {
        _branchRepository = branchRepository;
        _aiWidgetRepository = aiWidgetRepository;
        _contentBlockRepository = contentBlockRepository;
        _jobEnqueuer = jobEnqueuer;
        _unitOfWork = unitOfWork;
    }

    public async Task<SubmitCompanyContentResult> Handle(SubmitCompanyContentCommand request, CancellationToken cancellationToken)
    {
        var branch = await _branchRepository.GetByIdAsync(request.BranchId);
        if (branch == null || branch.CompanyId != request.CompanyId)
            throw new InvalidOperationException("Branch not found or does not belong to company.");

        var aiWidget = await _aiWidgetRepository.GetByBranchIdAsync(request.BranchId, cancellationToken);
        if (aiWidget == null)
            throw new InvalidOperationException("AI Widget is not set up for this branch. Call setup first.");

        var content = request.Content?.Trim() ?? "";
        if (string.IsNullOrEmpty(content))
            throw new InvalidOperationException("Content cannot be empty.");

        var jobId = Guid.NewGuid();
        var snippet = content.Length <= 500 ? content : content.Substring(0, 500);
        var block = new AiContentBlock(request.BranchId, aiWidget.Id, snippet, content, jobId);
        await _contentBlockRepository.AddAsync(block);
        await _unitOfWork.SaveChangesAsync();

        _jobEnqueuer.EnqueueIngest(jobId, aiWidget.TenantId, content, request.CompanyId, request.BranchId);

        return new SubmitCompanyContentResult
        {
            JobId = jobId,
            ContentBlockId = block.Id
        };
    }
}
