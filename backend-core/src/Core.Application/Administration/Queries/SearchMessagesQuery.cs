using MediatR;
using Core.Application.Administration.DTOs;
using Core.Application.DTOs;

namespace Core.Application.Administration.Queries;

public record SearchMessagesQuery(
    int Page = 1,
    int PageSize = 20,
    string? ContentSearch = null,
    Guid? SenderId = null,
    Guid? GroupId = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null) : IRequest<PaginatedResultDto<AdminMessageDto>>;
