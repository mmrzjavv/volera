using MediatR;

namespace Core.Application.Administration.Commands;

public record AdminDeleteMessageCommand(Guid MessageId, bool HardDelete, Guid AdminUserId) : IRequest<Unit>;
