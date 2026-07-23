using MediatR;
using Core.Application.Commands;

namespace WebAPI.Jobs;

public class StoryExpiryJob
{
    private readonly IMediator _mediator;

    public StoryExpiryJob(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task Process() => _mediator.Send(new ExpireStoriesCommand());
}
