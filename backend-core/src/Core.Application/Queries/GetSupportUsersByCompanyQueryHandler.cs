using MediatR;
using Core.Application.DTOs;
using Core.Application.Queries;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class GetSupportUsersByCompanyQueryHandler : IRequestHandler<GetSupportUsersByCompanyQuery, IEnumerable<SupportUserDto>>
{
    private readonly ISupportUserRepository _supportUserRepository;

    public GetSupportUsersByCompanyQueryHandler(ISupportUserRepository supportUserRepository)
    {
        _supportUserRepository = supportUserRepository;
    }

    public async Task<IEnumerable<SupportUserDto>> Handle(GetSupportUsersByCompanyQuery request, CancellationToken cancellationToken)
    {
        var users = await _supportUserRepository.GetByCompanyIdAsync(request.CompanyId, cancellationToken);
        return users.Select(u => new SupportUserDto
        {
            Id = u.Id,
            CompanyId = u.CompanyId,
            Username = u.Username,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Email = u.Email,
            PhoneNumber = u.PhoneNumber,
            Role = u.Role.ToRoleName()
        });
    }
}
