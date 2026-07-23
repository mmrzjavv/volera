using MediatR;

namespace Core.Application.Commands;

public class UpdateGroupProfileCommand : IRequest
{
    public Guid GroupId { get; set; }
    public Guid RequestingUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

