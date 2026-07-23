using MediatR;
using Core.Application.Commands;
using Core.Application.DTOs;
using Core.Domain.Interfaces;
using Core.Domain.Entities;

namespace Core.Application.Handlers;

public class SyncContactsCommandHandler : IRequestHandler<SyncContactsCommand, IEnumerable<ContactDto>>
{
    private readonly IContactRepository _contactRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SyncContactsCommandHandler(
        IContactRepository contactRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _contactRepository = contactRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ContactDto>> Handle(SyncContactsCommand request, CancellationToken cancellationToken)
    {
        var addedContacts = new List<(Contact Contact, User User)>();

        foreach (var phoneNumber in request.PhoneNumbers)
        {
             // Check if user exists
             var user = await _userRepository.GetByPhoneNumberAsync(phoneNumber);
             if (user != null)
             {
                 // Check if contact already exists
                 if (!await _contactRepository.ContactExistsAsync(request.UserId, user.Id))
                 {
                     var contact = new Contact(
                         request.UserId,
                         user.PhoneNumber,
                         user.Id,
                         $"{user.FirstName} {user.LastName}"
                     );
                     contact.UpdateStatus(ContactStatus.Accepted);
                     
                     await _contactRepository.AddAsync(contact);
                     addedContacts.Add((contact, user));
                 }
             }
        }

        if (addedContacts.Any())
        {
            await _unitOfWork.SaveChangesAsync();
        }
        
        return addedContacts.Select(item => new ContactDto
        {
            Id = item.Contact.Id,
            OwnerUserId = item.Contact.OwnerUserId,
            ContactUserId = item.Contact.ContactUserId,
            ContactName = item.Contact.ContactName,
            ContactPhoneNumber = item.Contact.ContactPhoneNumber,
            Status = item.Contact.Status.ToString(),
            CreatedAt = item.Contact.CreatedAt,
            UpdatedAt = item.Contact.UpdatedAt,
            ContactUser = new UserDto
            {
                Id = item.User.Id,
                FirstName = item.User.FirstName,
                LastName = item.User.LastName,
                Username = item.User.Username,
                PhoneNumber = item.User.PhoneNumber,
                ProfilePicture = item.User.ProfilePicture,
                CreatedAt = item.User.CreatedAt,
                UpdatedAt = item.User.UpdatedAt
            }
        });
    }
}
