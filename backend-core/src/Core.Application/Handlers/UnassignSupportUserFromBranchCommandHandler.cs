using MediatR;
using Core.Application.Commands;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class UnassignSupportUserFromBranchCommandHandler : IRequestHandler<UnassignSupportUserFromBranchCommand>
{
    private readonly ISupportUserBranchRepository _supportUserBranchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UnassignSupportUserFromBranchCommandHandler(
        ISupportUserBranchRepository supportUserBranchRepository,
        IUnitOfWork unitOfWork)
    {
        _supportUserBranchRepository = supportUserBranchRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UnassignSupportUserFromBranchCommand request, CancellationToken cancellationToken)
    {
        var assignment = await _supportUserBranchRepository.GetBySupportUserIdAndBranchIdAsync(request.SupportUserId, request.BranchId, cancellationToken);
        if (assignment == null)
            return;

        // Verify branch belongs to company
        if (assignment.Branch.CompanyId != request.CompanyId)
            throw new InvalidOperationException("Branch not found.");

        _supportUserBranchRepository.Delete(assignment);
        await _unitOfWork.SaveChangesAsync();
    }
}
