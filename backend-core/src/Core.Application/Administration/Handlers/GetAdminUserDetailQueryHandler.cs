using MediatR;
using Core.Application.Administration.DTOs;
using Core.Application.Administration.Queries;
using Core.Application.Interfaces;

namespace Core.Application.Administration.Handlers;

public class GetAdminUserDetailQueryHandler : IRequestHandler<GetAdminUserDetailQuery, AdminUserDetailDto?>
{
    private readonly IAdminReadRepository _adminReadRepository;

    public GetAdminUserDetailQueryHandler(IAdminReadRepository adminReadRepository)
    {
        _adminReadRepository = adminReadRepository;
    }

    public async Task<AdminUserDetailDto?> Handle(GetAdminUserDetailQuery request, CancellationToken cancellationToken)
    {
        return await _adminReadRepository.GetAdminUserDetailAsync(request.UserId, cancellationToken);
    }
}
