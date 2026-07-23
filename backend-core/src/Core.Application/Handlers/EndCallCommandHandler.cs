using MediatR;
using Core.Application.Commands;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class EndCallCommandHandler : IRequestHandler<EndCallCommand>
{
    private readonly ICallRepository _callRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;

    public EndCallCommandHandler(ICallRepository callRepository, IUnitOfWork unitOfWork, IMediator mediator)
    {
        _callRepository = callRepository;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
    }

    public async Task Handle(EndCallCommand request, CancellationToken cancellationToken)
    {
        var call = await _callRepository.GetByIdAsync(request.CallId);
        if (call == null)
            throw new KeyNotFoundException("Call not found.");

        if (call.CallerId != request.UserId && call.ReceiverId != request.UserId)
            throw new InvalidOperationException("Only participants can end the call.");

        call.End();
        _callRepository.Update(call);
        await _unitOfWork.SaveChangesAsync();

        // Publish domain events
        foreach (var domainEvent in call.DomainEvents)
        {
            await _mediator.Publish(domainEvent);
        }
        call.ClearDomainEvents();
    }
}