using MediatR;

namespace Core.Application.Administration.Commands;

public record RemoveUserLimitOverrideCommand(Guid UserId, string LimitKey, Guid AdminUserId) : IRequest<Unit>;
