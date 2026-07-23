using MediatR;

namespace Core.Application.Administration.Commands;

public record AdminUpdateUserCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    string? Email,
    string? Bio,
    Guid AdminUserId) : IRequest<Unit>;
