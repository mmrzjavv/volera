using MediatR;
using Core.Application.Administration.DTOs;

namespace Core.Application.Administration.Queries;

public record GetMessagesPerDayQuery(int Days = 30) : IRequest<IEnumerable<MessagesPerDayDto>>;
