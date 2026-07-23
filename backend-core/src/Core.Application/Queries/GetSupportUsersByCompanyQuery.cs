using MediatR;
using Core.Application.DTOs;

namespace Core.Application.Queries;

public class GetSupportUsersByCompanyQuery : IRequest<IEnumerable<SupportUserDto>>
{
    public Guid CompanyId { get; set; }
}
