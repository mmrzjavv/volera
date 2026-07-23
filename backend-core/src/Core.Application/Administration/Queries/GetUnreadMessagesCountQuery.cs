using MediatR;

namespace Core.Application.Administration.Queries;

public record GetUnreadMessagesCountQuery() : IRequest<int>;
