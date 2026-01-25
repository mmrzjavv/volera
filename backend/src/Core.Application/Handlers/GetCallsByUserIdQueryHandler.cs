using MediatR;
using Core.Application.Queries;
using Core.Application.DTOs;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class GetCallsByUserIdQueryHandler : IRequestHandler<GetCallsByUserIdQuery, IEnumerable<CallDto>>
{
    private readonly ICallRepository _callRepository;

    public GetCallsByUserIdQueryHandler(ICallRepository callRepository)
    {
        _callRepository = callRepository;
    }

    public async Task<IEnumerable<CallDto>> Handle(GetCallsByUserIdQuery request, CancellationToken cancellationToken)
    {
        var calls = await _callRepository.GetCallsByUserIdAsync(request.UserId);
        return calls.Select(c => new CallDto
        {
            Id = c.Id,
            CallerId = c.CallerId,
            CallerName = $"{c.Caller.FirstName} {c.Caller.LastName}",
            ReceiverId = c.ReceiverId,
            ReceiverName = $"{c.Receiver.FirstName} {c.Receiver.LastName}",
            StartTime = c.StartTime,
            EndTime = c.EndTime,
            Duration = c.Duration,
            Status = c.Status.ToString()
        });
    }
}