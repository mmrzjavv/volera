using MediatR;
using Core.Application.Administration.DTOs;
using Core.Application.Administration.Queries;
using Core.Application.Interfaces;

namespace Core.Application.Administration.Handlers;

public class GetMessagesPerDayQueryHandler : IRequestHandler<GetMessagesPerDayQuery, IEnumerable<MessagesPerDayDto>>
{
    private readonly IAdminReadRepository _adminReadRepository;

    public GetMessagesPerDayQueryHandler(IAdminReadRepository adminReadRepository)
    {
        _adminReadRepository = adminReadRepository;
    }

    public async Task<IEnumerable<MessagesPerDayDto>> Handle(GetMessagesPerDayQuery request, CancellationToken cancellationToken)
    {
        return await _adminReadRepository.GetMessagesPerDayAsync(request.Days, cancellationToken);
    }
}
