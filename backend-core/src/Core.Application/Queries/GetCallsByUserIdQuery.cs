using MediatR;
using Core.Application.DTOs;

namespace Core.Application.Queries;

public class GetCallsByUserIdQuery : IRequest<PaginatedResultDto<CallDto>>
{
    public Guid UserId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Term { get; set; } // Search by name or whatever
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? SortBy { get; set; } // Duration, StartTime
    public bool SortDescending { get; set; } = true;
}
