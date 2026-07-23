using MediatR;
using Core.Application.Administration.Queries;
using Core.Application.Interfaces;

namespace Core.Application.Administration.Handlers;

public class GetUnreadMessagesCountQueryHandler : IRequestHandler<GetUnreadMessagesCountQuery, int>
{
    private readonly IAdminReadRepository _adminReadRepository;

    public GetUnreadMessagesCountQueryHandler(IAdminReadRepository adminReadRepository)
    {
        _adminReadRepository = adminReadRepository;
    }

    public async Task<int> Handle(GetUnreadMessagesCountQuery request, CancellationToken cancellationToken)
    {
        return await _adminReadRepository.GetUnreadMessagesCountAsync(cancellationToken);
    }
}
