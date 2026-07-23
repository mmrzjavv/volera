using MediatR;
using Core.Application.DTOs;

namespace Core.Application.Queries;

public class SearchUserByPhoneNumberQuery : IRequest<UserDto?>
{
    public string PhoneNumber { get; set; } = string.Empty;
}
