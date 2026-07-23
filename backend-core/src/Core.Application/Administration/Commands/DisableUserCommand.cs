using MediatR;

namespace Core.Application.Administration.Commands;

public record DisableUserCommand(Guid UserId, Guid AdminUserId) : IRequest<Unit>;
