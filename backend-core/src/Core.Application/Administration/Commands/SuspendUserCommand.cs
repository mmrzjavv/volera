using MediatR;

namespace Core.Application.Administration.Commands;

public record SuspendUserCommand(Guid UserId, DateTime Until, Guid AdminUserId) : IRequest<Unit>;
