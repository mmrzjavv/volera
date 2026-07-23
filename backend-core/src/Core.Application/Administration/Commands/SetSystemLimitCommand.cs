using MediatR;

namespace Core.Application.Administration.Commands;

public record SetSystemLimitCommand(string LimitKey, decimal Value, Guid AdminUserId) : IRequest<Unit>;
