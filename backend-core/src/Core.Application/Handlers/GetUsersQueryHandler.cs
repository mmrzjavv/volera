using MediatR;
using Core.Application.Queries;
using Core.Application.DTOs;
using Core.Domain.Interfaces;
using Core.Application.Interfaces;
using AutoMapper;

namespace Core.Application.Handlers;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PaginatedResultDto<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IOnlineUserService _onlineUserService;
    private readonly IMapper _mapper;

    public GetUsersQueryHandler(IUserRepository userRepository, IOnlineUserService onlineUserService, IMapper mapper)
    {
        _userRepository = userRepository;
        _onlineUserService = onlineUserService;
        _mapper = mapper;
    }

    public async Task<PaginatedResultDto<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var (users, totalCount) = await _userRepository.GetUsersAsync(
            request.Page, 
            request.PageSize, 
            request.Term, 
            request.ExcludeUserId
        );

        var userDtos = _mapper.Map<List<UserDto>>(users);

        // Batch online status to avoid N+1 IsUserOnline calls
        var onlineUserIds = await _onlineUserService.GetOnlineUserIds();
        var onlineSet = onlineUserIds.ToHashSet();

        foreach (var dto in userDtos)
        {
            dto.IsOnline = onlineSet.Contains(dto.Id);
        }

        return new PaginatedResultDto<UserDto>
        {
            Items = userDtos,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}