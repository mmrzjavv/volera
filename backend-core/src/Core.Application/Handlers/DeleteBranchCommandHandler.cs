using MediatR;
using Core.Application.Commands;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class DeleteBranchCommandHandler : IRequestHandler<DeleteBranchCommand>
{
    private readonly IBranchRepository _branchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteBranchCommandHandler(IBranchRepository branchRepository, IUnitOfWork unitOfWork)
    {
        _branchRepository = branchRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteBranchCommand request, CancellationToken cancellationToken)
    {
        var branch = await _branchRepository.GetByIdAsync(request.BranchId);
        if (branch == null || branch.CompanyId != request.CompanyId)
            throw new InvalidOperationException("Branch not found.");

        branch.Deactivate();
        _branchRepository.Update(branch);
        await _unitOfWork.SaveChangesAsync();
    }
}
