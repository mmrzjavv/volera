using MediatR;

namespace Core.Application.Administration.Commands;

public record SetUserRoleCommand(Guid UserId, string Role, Guid AdminUserId) : IRequest<Unit>;
