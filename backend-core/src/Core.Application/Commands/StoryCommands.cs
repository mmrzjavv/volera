using MediatR;
using Core.Application.DTOs;

namespace Core.Application.Commands;

public class CreateStoryCommand : IRequest<Guid>
{
    public Guid UserId { get; set; }
    public List<CreateStoryItemDto> Items { get; set; } = new();
}

public class MarkStoryViewedCommand : IRequest
{
    public Guid StoryId { get; set; }
    public Guid ViewerUserId { get; set; }
}

public class DeleteStoryCommand : IRequest
{
    public Guid StoryId { get; set; }
    public Guid UserId { get; set; }
}

public class DeleteStoryItemCommand : IRequest
{
    public Guid ItemId { get; set; }
    public Guid UserId { get; set; }
}

public class ReplyToStoryItemCommand : IRequest<Guid>
{
    public Guid ItemId { get; set; }
    public Guid SenderId { get; set; }
    public required string Content { get; set; }
}

public class ExpireStoriesCommand : IRequest<int>
{
}
