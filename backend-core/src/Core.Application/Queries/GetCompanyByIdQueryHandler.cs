using MediatR;
using Core.Application.Queries;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class GetCompanyByIdQueryHandler : IRequestHandler<GetCompanyByIdQuery, CompanyDto?>
{
    private readonly ICompanyRepository _companyRepository;

    public GetCompanyByIdQueryHandler(ICompanyRepository companyRepository)
    {
        _companyRepository = companyRepository;
    }

    public async Task<CompanyDto?> Handle(GetCompanyByIdQuery request, CancellationToken cancellationToken)
    {
        var company = await _companyRepository.GetByIdAsync(request.CompanyId);
        if (company == null) return null;
        return new CompanyDto
        {
            Id = company.Id,
            Name = company.Name,
            MobileNumber = company.MobileNumber,
            Email = company.Email,
            Address = company.Address,
            LogoUrl = company.LogoUrl,
            IsActive = company.IsActive
        };
    }
}
