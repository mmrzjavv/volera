using MediatR;
using Core.Application.Queries;
using Core.Application.DTOs;
using Core.Domain.Interfaces;
using AutoMapper;

namespace Core.Application.Handlers;

public class SearchUserByPhoneNumberQueryHandler : IRequestHandler<SearchUserByPhoneNumberQuery, UserDto?>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public SearchUserByPhoneNumberQueryHandler(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<UserDto?> Handle(SearchUserByPhoneNumberQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByPhoneNumberAsync(request.PhoneNumber);

        if (user == null)
            return null;

        return _mapper.Map<UserDto>(user);
    }
}
