using MediatR;
using Core.Application.Administration.DTOs;

namespace Core.Application.Administration.Queries;

public record GetSystemLimitsQuery() : IRequest<IEnumerable<SystemLimitDto>>;
