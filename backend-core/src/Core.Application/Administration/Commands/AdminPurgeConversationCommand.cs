using MediatR;

namespace Core.Application.Administration.Commands;

public record AdminPurgeConversationCommand(string ConversationKey, Guid AdminUserId) : IRequest<int>;
