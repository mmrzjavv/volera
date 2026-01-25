using MediatR;
using Core.Application.Commands;
using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Core.Application.Interfaces;

namespace Core.Application.Handlers;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Guid>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<Guid> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // Check uniqueness
        if (!await _userRepository.IsUsernameUniqueAsync(request.Username))
            throw new InvalidOperationException("Username is already taken.");

        if (!await _userRepository.IsPhoneNumberUniqueAsync(request.PhoneNumber))
            throw new InvalidOperationException("Phone number is already registered.");

        // Hash password
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        // Create user
        var user = new User(request.FirstName, request.LastName, request.Username, request.PhoneNumber, passwordHash);

        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return user.Id;
    }
}