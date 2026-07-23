using MediatR;
using Core.Application.Commands;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class UpdateBranchCommandHandler : IRequestHandler<UpdateBranchCommand>
{
    private readonly IBranchRepository _branchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBranchCommandHandler(IBranchRepository branchRepository, IUnitOfWork unitOfWork)
    {
        _branchRepository = branchRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateBranchCommand request, CancellationToken cancellationToken)
    {
        var branch = await _branchRepository.GetByIdAsync(request.BranchId);
        if (branch == null || branch.CompanyId != request.CompanyId)
            throw new InvalidOperationException("Branch not found.");

        branch.Update(
            request.Name ?? branch.Name,
            request.Address,
            request.PhoneNumber,
            request.Email);
        _branchRepository.Update(branch);
        await _unitOfWork.SaveChangesAsync();
    }
}
