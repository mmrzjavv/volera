using MediatR;
using Core.Application.Commands;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class CreateBranchCommandHandler : IRequestHandler<CreateBranchCommand, Guid>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateBranchCommandHandler(
        ICompanyRepository companyRepository,
        IBranchRepository branchRepository,
        IUnitOfWork unitOfWork)
    {
        _companyRepository = companyRepository;
        _branchRepository = branchRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateBranchCommand request, CancellationToken cancellationToken)
    {
        var company = await _companyRepository.GetByIdAsync(request.CompanyId);
        if (company == null)
            throw new InvalidOperationException("Company not found.");

        var branch = new Branch(
            request.CompanyId,
            request.Name.Trim(),
            request.Address?.Trim(),
            request.PhoneNumber?.Trim(),
            request.Email?.Trim());
        await _branchRepository.AddAsync(branch);
        await _unitOfWork.SaveChangesAsync();
        return branch.Id;
    }
}
