using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Core.Domain.Interfaces;

namespace WebAPI.Hubs;

/// <summary>
/// SignalR hub for support users. Auth via SupportUser JWT. Support users can join branch groups to receive real-time messages.
/// </summary>
[Authorize(AuthenticationSchemes = "SupportUser")]
public class SupportHub : Hub
{
    private readonly ISupportUserBranchRepository _supportUserBranchRepository;
    private readonly ISupportUserRepository _supportUserRepository;

    public SupportHub(
        ISupportUserBranchRepository supportUserBranchRepository,
        ISupportUserRepository supportUserRepository)
    {
        _supportUserBranchRepository = supportUserBranchRepository;
        _supportUserRepository = supportUserRepository;
    }

    public override async Task OnConnectedAsync()
    {
        var supportUserIdClaim = Context.User?.FindFirst("supportUserId")?.Value;
        if (!string.IsNullOrEmpty(supportUserIdClaim) && Guid.TryParse(supportUserIdClaim, out var supportUserId))
        {
            var assignments = await _supportUserBranchRepository.GetBySupportUserIdAsync(supportUserId);
            foreach (var a in assignments)
                await Groups.AddToGroupAsync(Context.ConnectionId, "branch_" + a.BranchId);
        }
        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        return base.OnDisconnectedAsync(exception);
    }
}
