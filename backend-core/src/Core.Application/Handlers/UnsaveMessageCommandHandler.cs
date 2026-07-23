using MediatR;
using Core.Domain.Interfaces;
using Core.Application.Commands;

namespace Core.Application.Handlers;

public class UnsaveMessageCommandHandler : IRequestHandler<UnsaveMessageCommand>
{
    private readonly ISavedMessageRepository _savedMessageRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UnsaveMessageCommandHandler(ISavedMessageRepository savedMessageRepository, IUnitOfWork unitOfWork)
    {
        _savedMessageRepository = savedMessageRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UnsaveMessageCommand request, CancellationToken cancellationToken)
    {
        var savedMessage = await _savedMessageRepository.GetByUserAndMessageIdAsync(request.UserId, request.MessageId);
        if (savedMessage != null)
        {
            _savedMessageRepository.Delete(savedMessage);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
