using MediatR;
using Core.Application.Commands;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProfileCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            throw new KeyNotFoundException("User not found.");

        user.UpdateProfile(request.FirstName, request.LastName, request.ProfilePicture, request.Email, request.Bio);
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();
    }
}