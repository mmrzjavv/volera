using MediatR;
using Core.Application.DTOs;

namespace Core.Application.Queries;

public class GetUsersQuery : IRequest<PaginatedResultDto<UserDto>>
{
    public Guid? ExcludeUserId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Term { get; set; }
}