using MediatR;
using Core.Application.Commands;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class RejectCallCommandHandler : IRequestHandler<RejectCallCommand>
{
    private readonly ICallRepository _callRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;

    public RejectCallCommandHandler(ICallRepository callRepository, IUnitOfWork unitOfWork, IMediator mediator)
    {
        _callRepository = callRepository;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
    }

    public async Task Handle(RejectCallCommand request, CancellationToken cancellationToken)
    {
        var call = await _callRepository.GetByIdAsync(request.CallId);
        if (call == null)
            throw new KeyNotFoundException("Call not found.");

        if (call.ReceiverId != request.UserId)
            throw new InvalidOperationException("Only the receiver can reject the call.");

        call.Reject();
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