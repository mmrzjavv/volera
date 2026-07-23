using Core.Domain.Entities;
using Core.Domain.Interfaces;
using MediatR;

namespace Core.Application.Commands.SystemMessages;

public record CreateSystemMessageCommand(
    Guid AuthorId,
    string Title,
    string Content,
    DateTime? ExpiresAt) : IRequest<Guid>;

public class CreateSystemMessageCommandHandler : IRequestHandler<CreateSystemMessageCommand, Guid>
{
    private readonly ISystemMessageRepository _systemMessageRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSystemMessageCommandHandler(
        ISystemMessageRepository systemMessageRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _systemMessageRepository = systemMessageRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateSystemMessageCommand request, CancellationToken cancellationToken)
    {
        var author = await _userRepository.GetByIdAsync(request.AuthorId);
        if (author is null || (author.Role != UserRole.Admin && author.Role != UserRole.SuperAdmin))
        {
            throw new UnauthorizedAccessException("Admin access required");
        }

        var message = new Core.Domain.Entities.SystemMessage(request.Title, request.Content, request.AuthorId, request.ExpiresAt);

        await _systemMessageRepository.AddAsync(message);
        await _unitOfWork.SaveChangesAsync();

        return message.Id;
    }
}

