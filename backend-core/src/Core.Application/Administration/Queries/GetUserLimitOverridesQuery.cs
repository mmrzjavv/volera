using MediatR;
using Core.Application.Administration.DTOs;

namespace Core.Application.Administration.Queries;

public record GetUserLimitOverridesQuery(Guid UserId) : IRequest<IEnumerable<AdminLimitOverrideDto>>;
