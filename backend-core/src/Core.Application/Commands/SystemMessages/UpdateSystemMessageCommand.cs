using Core.Domain.Entities;
using Core.Domain.Interfaces;
using MediatR;

namespace Core.Application.Commands.SystemMessages;

public record UpdateSystemMessageCommand(
    Guid MessageId,
    Guid AuthorId,
    string Title,
    string Content,
    DateTime? ExpiresAt) : IRequest<Unit>;

public class UpdateSystemMessageCommandHandler : IRequestHandler<UpdateSystemMessageCommand, Unit>
{
    private readonly ISystemMessageRepository _systemMessageRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSystemMessageCommandHandler(
        ISystemMessageRepository systemMessageRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _systemMessageRepository = systemMessageRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdateSystemMessageCommand request, CancellationToken cancellationToken)
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

        message.Update(request.Title, request.Content, request.ExpiresAt);

        await _unitOfWork.SaveChangesAsync();
        return Unit.Value;
    }
}

