using MediatR;
using Core.Application.Administration.DTOs;

namespace Core.Application.Administration.Queries;

public record GetAdminConversationQuery(
    string ConversationKey,
    int Limit = 50,
    DateTime? Before = null) : IRequest<AdminConversationResultDto>;
