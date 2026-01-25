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
        // Check if caller or receiver has active call
        var activeCall = await _callRepository.GetActiveCallByUserIdAsync(request.CallerId);
        if (activeCall != null)
            throw new InvalidOperationException("Caller has an active call.");

        activeCall = await _callRepository.GetActiveCallByUserIdAsync(request.ReceiverId);
        if (activeCall != null)
            throw new InvalidOperationException("Receiver has an active call.");

        var caller = await _userRepository.GetByIdAsync(request.CallerId);
        var receiver = await _userRepository.GetByIdAsync(request.ReceiverId);
        if (caller == null || receiver == null)
            throw new KeyNotFoundException("User not found.");

        var call = new Call(request.CallerId, request.ReceiverId);
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