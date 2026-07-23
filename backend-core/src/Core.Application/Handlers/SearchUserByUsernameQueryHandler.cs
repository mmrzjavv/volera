using MediatR;
using Core.Application.Queries;
using Core.Application.DTOs;
using Core.Domain.Interfaces;
using AutoMapper;

namespace Core.Application.Handlers;

public class SearchUserByUsernameQueryHandler : IRequestHandler<SearchUserByUsernameQuery, UserDto?>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public SearchUserByUsernameQueryHandler(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<UserDto?> Handle(SearchUserByUsernameQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username.Trim());

        if (user == null)
            return null;

        return _mapper.Map<UserDto>(user);
    }
}
