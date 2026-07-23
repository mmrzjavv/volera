using MediatR;
using Core.Application.Administration.DTOs;

namespace Core.Application.Administration.Queries;

public record GetTableRowCountsQuery() : IRequest<TableRowCountsDto>;
