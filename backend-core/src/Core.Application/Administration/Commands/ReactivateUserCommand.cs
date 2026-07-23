using MediatR;

namespace Core.Application.Administration.Commands;

public record ReactivateUserCommand(Guid UserId, Guid AdminUserId) : IRequest<Unit>;
