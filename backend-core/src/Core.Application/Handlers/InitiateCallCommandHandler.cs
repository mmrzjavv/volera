using MediatR;
using Core.Application.Commands;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class InitiateCallCommandHandler : IRequestHandler<InitiateCallCommand, Guid>
{
    private readonly ICallRepository _callRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;

    public InitiateCallCommandHandler(ICallRepository callRepository, IUserRepository userRepository, IUnitOfWork unitOfWork, IMediator mediator)
    {
        _callRepository = callRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
    }

    public async Task<Guid> Handle(InitiateCallCommand request, CancellationToken cancellationToken)
    {
        var caller = await _userRepository.GetByIdAsync(request.CallerId);
        var receiver = await _userRepository.GetByIdAsync(request.ReceiverId);
        if (caller == null || receiver == null)
            throw new KeyNotFoundException("User not found.");

        // Get active calls for both users before making any changes
        var callerActiveCall = await _callRepository.GetActiveCallByUserIdAsync(request.CallerId);
        var receiverActiveCall = await _callRepository.GetActiveCallByUserIdAsync(request.ReceiverId);

        // End any CONNECTED calls (ongoing calls) for caller and receiver
        // This allows ringing calls to continue, but ends actual ongoing calls
        if (callerActiveCall != null && callerActiveCall.Status == CallStatus.Connected)
        {
            callerActiveCall.End();
            _callRepository.Update(callerActiveCall);
            // Publish events for ended calls before creating new one
            foreach (var domainEvent in callerActiveCall.DomainEvents)
            {
                await _mediator.Publish(domainEvent);
            }
            callerActiveCall.ClearDomainEvents();
        }

        if (receiverActiveCall != null && receiverActiveCall.Status == CallStatus.Connected)
        {
            receiverActiveCall.End();
            _callRepository.Update(receiverActiveCall);
            // Publish events for ended calls before creating new one
            foreach (var domainEvent in receiverActiveCall.DomainEvents)
            {
                await _mediator.Publish(domainEvent);
            }
            receiverActiveCall.ClearDomainEvents();
        }

        // Reject any existing ringing call for the caller (same or different receiver).
        // Leaving a stale Ringing row caused the UI to show Incoming while Accept hit a non-ringing callId.
        if (callerActiveCall != null && callerActiveCall.Status == CallStatus.Ringing)
        {
            callerActiveCall.Reject();
            _callRepository.Update(callerActiveCall);
            foreach (var domainEvent in callerActiveCall.DomainEvents)
            {
                await _mediator.Publish(domainEvent);
            }
            callerActiveCall.ClearDomainEvents();
        }

        // Replace an existing ringing call for the receiver so only one active invite remains.
        if (receiverActiveCall != null &&
            receiverActiveCall.Status == CallStatus.Ringing &&
            receiverActiveCall.Id != callerActiveCall?.Id)
        {
            receiverActiveCall.MarkAsMissed();
            _callRepository.Update(receiverActiveCall);
            foreach (var domainEvent in receiverActiveCall.DomainEvents)
            {
                await _mediator.Publish(domainEvent);
            }
            receiverActiveCall.ClearDomainEvents();
        }

        // Save all changes at once
        await _unitOfWork.SaveChangesAsync();

        // Now create the new call
        var call = new Call(request.CallerId, request.ReceiverId, request.IsVideo);
        await _callRepository.AddAsync(call);
        await _unitOfWork.SaveChangesAsync();

        // Publish domain events
        foreach (var domainEvent in call.DomainEvents)
        {
            await _mediator.Publish(domainEvent);
        }
        call.ClearDomainEvents();

        return call.Id;
    }
}