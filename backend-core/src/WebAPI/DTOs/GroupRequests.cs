namespace WebAPI.DTOs;

public class CreateGroupRequest
{
    public required string Name { get; set; }
    public List<Guid> MemberIds { get; set; } = new();
}

public class AddMemberRequest
{
    public Guid MemberId { get; set; }
}

public class ChangeGroupAdminRequest
{
    public Guid NewAdminId { get; set; }
}

public class UpdateGroupProfileRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ProfilePictureUrl { get; set; }
}
