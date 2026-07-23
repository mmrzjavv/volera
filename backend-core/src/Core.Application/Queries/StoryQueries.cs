using MediatR;
using Core.Application.DTOs;

namespace Core.Application.Queries;

public class GetStoryFeedQuery : IRequest<List<StoryRingDto>>
{
    public Guid UserId { get; set; }
}

public class GetUserStoriesQuery : IRequest<List<StoryDto>>
{
    public Guid ViewerUserId { get; set; }
    public Guid TargetUserId { get; set; }
}

public class GetStoryViewersQuery : IRequest<List<StoryViewerDto>>
{
    public Guid StoryId { get; set; }
    public Guid RequesterId { get; set; }
}
