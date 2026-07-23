using Core.Domain.Entities;
using Core.Domain.Interfaces;
using MediatR;

namespace Core.Application.Commands.SystemMessages;

public record MarkSystemMessageReadCommand(
    Guid MessageId,
    Guid UserId) : IRequest<Unit>;

public class MarkSystemMessageReadCommandHandler : IRequestHandler<MarkSystemMessageReadCommand, Unit>
{
    private readonly ISystemMessageRepository _systemMessageRepository;
    private readonly ISystemMessageReadRepository _systemMessageReadRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MarkSystemMessageReadCommandHandler(
        ISystemMessageRepository systemMessageRepository,
        ISystemMessageReadRepository systemMessageReadRepository,
        IUnitOfWork unitOfWork)
    {
        _systemMessageRepository = systemMessageRepository;
        _systemMessageReadRepository = systemMessageReadRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(MarkSystemMessageReadCommand request, CancellationToken cancellationToken)
    {
        var message = await _systemMessageRepository.GetByIdAsync(request.MessageId);
        if (message is null)
        {
            throw new KeyNotFoundException("System message not found");
        }

        var alreadyRead = await _systemMessageReadRepository.HasReadAsync(request.MessageId, request.UserId, cancellationToken);
        if (!alreadyRead)
        {
            var read = new SystemMessageRead(request.MessageId, request.UserId);
            await _systemMessageReadRepository.AddAsync(read);
            await _unitOfWork.SaveChangesAsync();
        }

        return Unit.Value;
    }
}

