using MediatR;
using Core.Application.Commands;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class DeleteSupportUserCommandHandler : IRequestHandler<DeleteSupportUserCommand>
{
    private readonly ISupportUserRepository _supportUserRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSupportUserCommandHandler(ISupportUserRepository supportUserRepository, IUnitOfWork unitOfWork)
    {
        _supportUserRepository = supportUserRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteSupportUserCommand request, CancellationToken cancellationToken)
    {
        var supportUser = await _supportUserRepository.GetByIdAsync(request.SupportUserId);
        if (supportUser == null || supportUser.CompanyId != request.CompanyId)
            throw new InvalidOperationException("Support user not found.");

        supportUser.Deactivate();
        _supportUserRepository.Update(supportUser);
        await _unitOfWork.SaveChangesAsync();
    }
}
