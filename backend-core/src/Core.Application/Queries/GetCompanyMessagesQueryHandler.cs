using MediatR;
using Core.Application.Queries;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class GetCompanyMessagesQueryHandler : IRequestHandler<GetCompanyMessagesQuery, IEnumerable<Message>>
{
    private readonly ISupportUserRepository _supportUserRepository;
    private readonly ISupportUserBranchRepository _supportUserBranchRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IMessageRepository _messageRepository;

    public GetCompanyMessagesQueryHandler(
        ISupportUserRepository supportUserRepository,
        ISupportUserBranchRepository supportUserBranchRepository,
        IBranchRepository branchRepository,
        IMessageRepository messageRepository)
    {
        _supportUserRepository = supportUserRepository;
        _supportUserBranchRepository = supportUserBranchRepository;
        _branchRepository = branchRepository;
        _messageRepository = messageRepository;
    }

    public async Task<IEnumerable<Message>> Handle(GetCompanyMessagesQuery request, CancellationToken cancellationToken)
    {
        var supportUser = await _supportUserRepository.GetByIdAsync(request.SupportUserId);
        if (supportUser == null)
            return Array.Empty<Message>();

        var branch = await _branchRepository.GetByIdAsync(request.BranchId);
        if (branch == null || branch.CompanyId != supportUser.CompanyId)
            return Array.Empty<Message>();

        if (supportUser.Role.CanViewAllCompanyMessages())
            return await _messageRepository.GetByBranchIdAsync(request.BranchId, request.Limit, request.Before, cancellationToken);

        var assignment = await _supportUserBranchRepository.GetBySupportUserIdAndBranchIdAsync(request.SupportUserId, request.BranchId, cancellationToken);
        if (assignment == null)
            return Array.Empty<Message>();

        return await _messageRepository.GetByBranchIdAsync(request.BranchId, request.Limit, request.Before, cancellationToken);
    }
}
