using MediatR;
using System;
using Core.Application.DTOs;

namespace Core.Application.Queries;

public record GetSavedMessagesQuery(Guid UserId, int Page = 1, int PageSize = 20) : IRequest<PaginatedResultDto<SavedMessageDto>>;
