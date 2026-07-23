using MediatR;
using Core.Application.Commands;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class UpdateSupportUserCommandHandler : IRequestHandler<UpdateSupportUserCommand>
{
    private readonly ISupportUserRepository _supportUserRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSupportUserCommandHandler(ISupportUserRepository supportUserRepository, IUnitOfWork unitOfWork)
    {
        _supportUserRepository = supportUserRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateSupportUserCommand request, CancellationToken cancellationToken)
    {
        var supportUser = await _supportUserRepository.GetByIdAsync(request.SupportUserId);
        if (supportUser == null || supportUser.CompanyId != request.CompanyId)
            throw new InvalidOperationException("Support user not found.");

        supportUser.UpdateProfile(
            request.FirstName ?? supportUser.FirstName,
            request.LastName ?? supportUser.LastName,
            request.Email,
            request.PhoneNumber);
        _supportUserRepository.Update(supportUser);
        await _unitOfWork.SaveChangesAsync();
    }
}
