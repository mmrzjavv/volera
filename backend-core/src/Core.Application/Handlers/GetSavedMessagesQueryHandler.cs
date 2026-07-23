using MediatR;
using Core.Domain.Interfaces;
using Core.Application.Queries;
using Core.Application.DTOs;
using AutoMapper;

namespace Core.Application.Handlers;

public class GetSavedMessagesQueryHandler : IRequestHandler<GetSavedMessagesQuery, PaginatedResultDto<SavedMessageDto>>
{
    private readonly ISavedMessageRepository _savedMessageRepository;
    private readonly IMapper _mapper;

    public GetSavedMessagesQueryHandler(ISavedMessageRepository savedMessageRepository, IMapper mapper)
    {
        _savedMessageRepository = savedMessageRepository;
        _mapper = mapper;
    }

    public async Task<PaginatedResultDto<SavedMessageDto>> Handle(GetSavedMessagesQuery request, CancellationToken cancellationToken)
    {
        var savedMessages = await _savedMessageRepository.GetByUserIdAsync(request.UserId, request.Page, request.PageSize);
        var totalCount = await _savedMessageRepository.GetCountByUserIdAsync(request.UserId);

        var savedMessageDtos = savedMessages
            .Select(sm =>
            {
                var dto = _mapper.Map<SavedMessageDto>(sm);
                if (dto.Message != null)
                {
                    dto.Message.IsSaved = true;
                }
                return dto;
            });

        return new PaginatedResultDto<SavedMessageDto>
        {
            Items = savedMessageDtos,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
