using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Core.Application.DTOs;
using Core.Application.Queries;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class GetSupportBranchMessagesQueryHandler : IRequestHandler<GetSupportBranchMessagesQuery, IEnumerable<BranchMessageDto>>
{
    private readonly ISupportUserRepository _supportUserRepository;
    private readonly ISupportUserBranchRepository _supportUserBranchRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IGuestRepository _guestRepository;
    private readonly ICompanyClientRepository _companyClientRepository;

    public GetSupportBranchMessagesQueryHandler(
        ISupportUserRepository supportUserRepository,
        ISupportUserBranchRepository supportUserBranchRepository,
        IBranchRepository branchRepository,
        IMessageRepository messageRepository,
        IGuestRepository guestRepository,
        ICompanyClientRepository companyClientRepository)
    {
        _supportUserRepository = supportUserRepository;
        _supportUserBranchRepository = supportUserBranchRepository;
        _branchRepository = branchRepository;
        _messageRepository = messageRepository;
        _guestRepository = guestRepository;
        _companyClientRepository = companyClientRepository;
    }

    public async Task<IEnumerable<BranchMessageDto>> Handle(GetSupportBranchMessagesQuery request, CancellationToken cancellationToken)
    {
        var supportUser = await _supportUserRepository.GetByIdAsync(request.SupportUserId);
        if (supportUser == null)
            return Array.Empty<BranchMessageDto>();

        var branch = await _branchRepository.GetByIdAsync(request.BranchId);
        if (branch == null || branch.CompanyId != supportUser.CompanyId)
            return Array.Empty<BranchMessageDto>();

        if (!supportUser.Role.CanViewAllCompanyMessages())
        {
            var assignment = await _supportUserBranchRepository.GetBySupportUserIdAndBranchIdAsync(request.SupportUserId, request.BranchId, cancellationToken);
            if (assignment == null)
                return Array.Empty<BranchMessageDto>();
        }

        var messages = (await _messageRepository.GetByBranchIdAsync(request.BranchId, request.Limit, request.Before, cancellationToken)).ToList();
        if (messages.Count == 0)
            return Array.Empty<BranchMessageDto>();

        var clientSenderIds = messages
            .Where(m => m.Sender != null && (m.Sender.Role.IsGuest() || m.Sender.Role.IsCompanyClient()))
            .Select(m => m.SenderId)
            .Distinct()
            .ToList();

        var guests = clientSenderIds.Count > 0
            ? await _guestRepository.GetByUserIdsAsync(messages.Where(m => m.Sender?.Role.IsGuest() == true).Select(m => m.SenderId).Distinct(), cancellationToken)
            : new Dictionary<Guid, Guest>();
        var companyClients = clientSenderIds.Count > 0
            ? await _companyClientRepository.GetByUserIdsAsync(messages.Where(m => m.Sender?.Role.IsCompanyClient() == true).Select(m => m.SenderId).Distinct(), cancellationToken)
            : new Dictionary<Guid, CompanyClient>();

        return messages.Select(m => MapToDto(m, guests, companyClients));
    }

    private static BranchMessageDto MapToDto(Message m, IReadOnlyDictionary<Guid, Guest> guests, IReadOnlyDictionary<Guid, CompanyClient> companyClients)
    {
        var dto = new BranchMessageDto
        {
            Id = m.Id,
            SenderId = m.SenderId,
            SupportSenderId = m.SupportSenderId,
            TargetReceiverUserId = m.TargetReceiverUserId,
            Content = m.Content ?? string.Empty,
            AttachmentUrl = m.AttachmentUrl,
            AttachmentType = m.AttachmentType,
            SentAt = m.SentAt,
            ReplyToMessageId = m.ReplyToMessageId,
            MessageReactions = (m.MessageReactions ?? new List<MessageReaction>()).Select(r => new MessageReactionDto
            {
                UserId = r.UserId,
                UserName = r.User != null ? TrimOrNull($"{r.User.FirstName} {r.User.LastName}".Trim()) ?? r.User.Username : null,
                SupportUserId = r.SupportUserId,
                SupportUserName = r.SupportUser != null ? TrimOrNull($"{r.SupportUser.FirstName} {r.SupportUser.LastName}".Trim()) ?? r.SupportUser.Username : null,
                Emoji = r.Emoji ?? string.Empty
            }).ToList()
        };

        if (m.Sender != null)
        {
            var firstName = m.Sender.FirstName;
            var lastName = m.Sender.LastName;
            var email = (string?)null;
            var phoneNumber = (string?)null;

            if (guests.TryGetValue(m.SenderId, out var guest))
            {
                firstName = guest.FirstName ?? firstName;
                lastName = guest.LastName ?? lastName;
                email = guest.Email;
                phoneNumber = guest.Mobile;
            }
            else if (companyClients.TryGetValue(m.SenderId, out var client))
            {
                firstName = client.FirstName ?? firstName;
                lastName = client.LastName ?? lastName;
                email = client.Email;
                phoneNumber = client.Mobile;
            }
            else
            {
                phoneNumber = m.Sender.PhoneNumber;
                if (phoneNumber != null && (phoneNumber.StartsWith("g", StringComparison.Ordinal) || phoneNumber.StartsWith("c", StringComparison.Ordinal)))
                    phoneNumber = null;
                email = m.Sender.Email;
            }

            dto.Sender = new BranchMessageSenderDto
            {
                Id = m.Sender.Id,
                FirstName = string.IsNullOrWhiteSpace(firstName) ? null : firstName,
                LastName = string.IsNullOrWhiteSpace(lastName) ? null : lastName,
                Username = m.Sender.Username,
                Email = string.IsNullOrWhiteSpace(email) ? null : email,
                PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber,
                Role = m.Sender.Role.ToRoleName()
            };
        }

        if (m.SupportSender != null)
        {
            dto.SupportSender = new BranchMessageSupportSenderDto
            {
                Id = m.SupportSender.Id,
                FirstName = m.SupportSender.FirstName,
                LastName = m.SupportSender.LastName,
                Username = m.SupportSender.Username
            };
        }

        if (m.ReplyToMessage != null)
        {
            dto.ReplyToMessage = new ReplyToMessagePreviewDto
            {
                Id = m.ReplyToMessage.Id,
                SenderId = m.ReplyToMessage.SenderId,
                SenderName = m.ReplyToMessage.Sender != null ? TrimOrNull($"{m.ReplyToMessage.Sender.FirstName} {m.ReplyToMessage.Sender.LastName}".Trim()) ?? m.ReplyToMessage.Sender.Username ?? "" : "",
                ContentSnippet = BuildContentSnippet(m.ReplyToMessage.Content),
                DeletedAt = m.ReplyToMessage.DeletedAt
            };
        }

        return dto;
    }

    private static string? TrimOrNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static string BuildContentSnippet(string? content, int maxLength = 60)
    {
        if (string.IsNullOrEmpty(content)) return "";
        return content.Length <= maxLength ? content : content[..maxLength] + "…";
    }
}
