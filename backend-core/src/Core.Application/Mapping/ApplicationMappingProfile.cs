using AutoMapper;
using Core.Application.DTOs;
using Core.Domain.Entities;

namespace Core.Application.Mapping;

/// <summary>
/// Central AutoMapper profile for application-layer DTO mappings.
/// Additional bounded-context-specific profiles can be added and registered alongside this one.
/// </summary>
public class ApplicationMappingProfile : Profile
{
    public ApplicationMappingProfile()
    {
        // Message -> MessageDto (used for read models and notifications)
        CreateMap<Message, MessageDto>()
            .ForMember(d => d.SenderId, o => o.MapFrom(s => s.SenderId))
            .ForMember(d => d.ReceiverId, o => o.MapFrom(s => s.ReceiverId))
            .ForMember(d => d.GroupId, o => o.MapFrom(s => s.GroupId))
            .ForMember(d => d.Content, o => o.MapFrom(s => s.Content))
            .ForMember(d => d.SentAt, o => o.MapFrom(s => s.SentAt))
            .ForMember(d => d.IsRead, o => o.MapFrom(s => s.IsRead))
            .ForMember(d => d.IsEdited, o => o.MapFrom(s => s.IsEdited))
            .ForMember(d => d.DeletedAt, o => o.MapFrom(s => s.DeletedAt))
            .ForMember(d => d.AttachmentUrl, o => o.MapFrom(s => s.AttachmentUrl))
            .ForMember(d => d.AttachmentType, o => o.MapFrom(s => s.AttachmentType))
            .ForMember(d => d.ReplyToMessageId, o => o.MapFrom(s => s.ReplyToMessageId))
            .ForMember(d => d.ForwardedFromMessageId, o => o.MapFrom(s => s.ForwardedFromMessageId))
            .ForMember(d => d.ForwardedAt, o => o.MapFrom(s => s.ForwardedAt))
            .ForMember(d => d.IsPinned, o => o.MapFrom(s => s.IsPinned))
            .ForMember(d => d.PinnedAt, o => o.MapFrom(s => s.PinnedAt))
            .ForMember(d => d.PinnedByUserId, o => o.MapFrom(s => s.PinnedByUserId))
            .ForMember(d => d.ClientMessageId, o => o.MapFrom(s => s.ClientMessageId))
            .ForMember(d => d.SignatureDisplayName, o => o.MapFrom(s => s.SignatureDisplayName))
            .ForMember(d => d.ViewCount, o => o.MapFrom(s => s.ViewCount))
            .ForMember(d => d.SendAsChannelId, o => o.MapFrom(s => s.SendAsChannelId))
            // Flags that are computed elsewhere remain with their defaults
            .ForMember(d => d.IsSaved, o => o.Ignore())
            .ForMember(d => d.ReplyToMessagePreview, o => o.Ignore())
            .ForMember(d => d.ReplyToStoryItemPreview, o => o.Ignore())
            .ForMember(d => d.Reactions, o => o.Ignore())
            .ForMember(d => d.SendAsChannelName, o => o.Ignore())
            .ForMember(d => d.SendAsChannelProfilePictureUrl, o => o.Ignore());

        // SystemMessage -> SystemMessageDto
        CreateMap<SystemMessage, SystemMessageDto>()
            .ForMember(d => d.IsRead, o => o.Ignore());

        // User -> UserDto
        CreateMap<User, UserDto>();

        // Contact -> ContactDto (without nested ContactUser mapping)
        CreateMap<Contact, ContactDto>()
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.ContactUser, o => o.Ignore());

        // SavedMessage -> SavedMessageDto
        CreateMap<SavedMessage, SavedMessageDto>()
            .ForMember(d => d.Message, o => o.MapFrom(s => s.Message));
    }
}

