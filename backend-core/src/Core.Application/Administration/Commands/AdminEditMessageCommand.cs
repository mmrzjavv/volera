using MediatR;

namespace Core.Application.Administration.Commands;

public record AdminEditMessageCommand(Guid MessageId, string NewContent, Guid AdminUserId) : IRequest<Unit>;
