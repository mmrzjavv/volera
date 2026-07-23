using MediatR;

namespace Core.Application.Administration.Commands;

public record SetUserLimitOverrideCommand(Guid UserId, string LimitKey, decimal Value, Guid AdminUserId) : IRequest<Unit>;
