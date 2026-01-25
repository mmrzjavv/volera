using MediatR;
using Core.Application.Queries;
using Core.Application.DTOs;
using Core.Domain.Interfaces;
using Core.Application.Interfaces;

namespace Core.Application.Handlers;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, List<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IOnlineUserService _onlineUserService;

    public GetUsersQueryHandler(IUserRepository userRepository, IOnlineUserService onlineUserService)
    {
        _userRepository = userRepository;
        _onlineUserService = onlineUserService;
    }

    public async Task<List<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetAllAsync();
        var onlineUserIds = await _onlineUserService.GetOnlineUserIds();
        var onlineSet = new HashSet<Guid>(onlineUserIds);
        var userDtos = users.Select(u => new UserDto
        {
            Id = u.Id,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Username = u.Username,
            PhoneNumber = u.PhoneNumber,
            ProfilePicture = u.ProfilePicture,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt,
            IsOnline = onlineSet.Contains(u.Id)
        }).ToList();

        if (request.ExcludeUserId.HasValue)
        {
            userDtos = userDtos.Where(u => u.Id != request.ExcludeUserId.Value).ToList();
        }

        return userDtos;
    }
}