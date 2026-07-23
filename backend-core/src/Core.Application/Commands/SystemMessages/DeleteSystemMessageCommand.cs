using Core.Domain.Entities;
using Core.Domain.Interfaces;
using MediatR;

namespace Core.Application.Commands.SystemMessages;

public record DeleteSystemMessageCommand(
    Guid MessageId,
    Guid AuthorId) : IRequest<Unit>;

public class DeleteSystemMessageCommandHandler : IRequestHandler<DeleteSystemMessageCommand, Unit>
{
    private readonly ISystemMessageRepository _systemMessageRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSystemMessageCommandHandler(
        ISystemMessageRepository systemMessageRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _systemMessageRepository = systemMessageRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteSystemMessageCommand request, CancellationToken cancellationToken)
    {
        var author = await _userRepository.GetByIdAsync(request.AuthorId);
        if (author is null || (author.Role != UserRole.Admin && author.Role != UserRole.SuperAdmin))
        {
            throw new UnauthorizedAccessException("Admin access required");
        }

        var message = await _systemMessageRepository.GetByIdAsync(request.MessageId);
        if (message is null)
        {
            throw new KeyNotFoundException("System message not found");
        }

        message.Deactivate();

        await _unitOfWork.SaveChangesAsync();
        return Unit.Value;
    }
}

