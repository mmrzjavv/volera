using MediatR;
using Core.Application.Queries;
using Core.Application.DTOs;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class GetCallsByUserIdQueryHandler : IRequestHandler<GetCallsByUserIdQuery, PaginatedResultDto<CallDto>>
{
    private readonly ICallRepository _callRepository;

    public GetCallsByUserIdQueryHandler(ICallRepository callRepository)
    {
        _callRepository = callRepository;
    }

    public async Task<PaginatedResultDto<CallDto>> Handle(GetCallsByUserIdQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _callRepository.GetCallsByUserIdAsync(
            request.UserId,
            request.Page,
            request.PageSize,
            request.Term,
            request.DateFrom,
            request.DateTo,
            request.SortBy,
            request.SortDescending
        );

        var dtos = items.Select(c => new CallDto
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

        return new PaginatedResultDto<CallDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}