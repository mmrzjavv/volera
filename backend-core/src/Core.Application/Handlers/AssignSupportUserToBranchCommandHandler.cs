using MediatR;
using Core.Application.Commands;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class AssignSupportUserToBranchCommandHandler : IRequestHandler<AssignSupportUserToBranchCommand>
{
    private readonly ISupportUserRepository _supportUserRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly ISupportUserBranchRepository _supportUserBranchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AssignSupportUserToBranchCommandHandler(
        ISupportUserRepository supportUserRepository,
        IBranchRepository branchRepository,
        ISupportUserBranchRepository supportUserBranchRepository,
        IUnitOfWork unitOfWork)
    {
        _supportUserRepository = supportUserRepository;
        _branchRepository = branchRepository;
        _supportUserBranchRepository = supportUserBranchRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AssignSupportUserToBranchCommand request, CancellationToken cancellationToken)
    {
        var supportUser = await _supportUserRepository.GetByIdAsync(request.SupportUserId);
        if (supportUser == null || supportUser.CompanyId != request.CompanyId)
            throw new InvalidOperationException("Support user not found.");

        var branch = await _branchRepository.GetByIdAsync(request.BranchId);
        if (branch == null || branch.CompanyId != request.CompanyId)
            throw new InvalidOperationException("Branch not found.");

        var existing = await _supportUserBranchRepository.GetBySupportUserIdAndBranchIdAsync(request.SupportUserId, request.BranchId, cancellationToken);
        if (existing != null)
            return; // already assigned

        var assignment = new SupportUserBranch(request.SupportUserId, request.BranchId);
        await _supportUserBranchRepository.AddAsync(assignment);
        await _unitOfWork.SaveChangesAsync();
    }
}
