using MediatR;
using Core.Application.Commands;
using Core.Domain.Interfaces;
using Core.Application.Interfaces;

namespace Core.Application.Handlers;

public class DeleteOfflineUsersCommandHandler : IRequestHandler<DeleteOfflineUsersCommand, int>
{
    private readonly IUserRepository _userRepository;
    private readonly ICallRepository _callRepository;
    private readonly IOnlineUserService _onlineUserService;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteOfflineUsersCommandHandler(
        IUserRepository userRepository,
        ICallRepository callRepository,
        IOnlineUserService onlineUserService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _callRepository = callRepository;
        _onlineUserService = onlineUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(DeleteOfflineUsersCommand request, CancellationToken cancellationToken)
    {
        // Get all users
        var allUsers = await _userRepository.GetAllAsync();
        
        // Get online user IDs
        var onlineUserIds = await _onlineUserService.GetOnlineUserIds();
        var onlineSet = new HashSet<Guid>(onlineUserIds);
        
        // Find offline users
        var offlineUsers = allUsers.Where(u => !onlineSet.Contains(u.Id)).ToList();
        
        // Delete calls associated with offline users first (due to foreign key constraints)
        foreach (var user in offlineUsers)
        {
            var userCalls = await _callRepository.GetCallsByUserIdAsync(user.Id);
            foreach (var call in userCalls)
            {
                _callRepository.Delete(call);
            }
        }
        
        // Delete offline users
        foreach (var user in offlineUsers)
        {
            _userRepository.Delete(user);
        }
        
        // Save changes
        await _unitOfWork.SaveChangesAsync();
        
        return offlineUsers.Count;
    }
}
