using System.Text.RegularExpressions;
using MediatR;
using Core.Application.Commands;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class AddContactCommandHandler : IRequestHandler<AddContactCommand, Guid>
{
    private readonly IContactRepository _contactRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddContactCommandHandler(
        IContactRepository contactRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _contactRepository = contactRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(AddContactCommand request, CancellationToken cancellationToken)
    {
        User? contactUser = null;
        string contactPhoneNumber = string.Empty;
        string? contactName = null;
        var identifier = request.ContactIdentifier.Trim();

        // Resolve by Guid, username, or phone number
        if (Guid.TryParse(identifier, out Guid contactUserId))
        {
            contactUser = await _userRepository.GetByIdAsync(contactUserId);
            if (contactUser == null)
            {
                throw new KeyNotFoundException("User not found with the provided ID.");
            }
            contactPhoneNumber = contactUser.PhoneNumber;
        }
        else
        {
            contactUser = await _userRepository.GetByUsernameAsync(identifier);
            if (contactUser != null)
            {
                contactPhoneNumber = contactUser.PhoneNumber;
            }
            else
            {
                contactUser = await _userRepository.GetByPhoneNumberAsync(identifier);
                contactPhoneNumber = contactUser?.PhoneNumber ?? identifier;

                if (contactUser == null)
                {
                    // Offline contacts are phone-only; usernames must resolve to a registered user
                    if (!LooksLikePhoneNumber(identifier))
                    {
                        throw new KeyNotFoundException("User not found with the provided username or phone number.");
                    }
                    contactName = identifier;
                }
            }
        }

        // Check if already exists
        if (contactUser != null)
        {
            if (await _contactRepository.ContactExistsAsync(request.OwnerUserId, contactUser.Id))
                throw new InvalidOperationException("Contact already exists.");
        }
        else
        {
            if (await _contactRepository.ContactExistsAsync(request.OwnerUserId, contactPhoneNumber))
                throw new InvalidOperationException("Contact already exists.");
        }

        var resolvedName = !string.IsNullOrWhiteSpace(request.ContactName)
            ? request.ContactName.Trim()
            : contactUser == null
                ? contactName
                : $"{contactUser.FirstName} {contactUser.LastName}";

        // Create Contact
        var contact = new Contact(
            request.OwnerUserId,
            contactUser?.PhoneNumber ?? contactPhoneNumber,
            contactUser?.Id,
            resolvedName
        );

        // Default to Accepted as there is no explicit Accept endpoint requested
        contact.UpdateStatus(ContactStatus.Accepted);

        await _contactRepository.AddAsync(contact);
        await _unitOfWork.SaveChangesAsync();

        return contact.Id;
    }

    /// <summary>
    /// Distinguishes phone-like identifiers (offline contacts allowed) from usernames.
    /// Accepts local and E.164 styles; usernames are not digit-only.
    /// </summary>
    private static bool LooksLikePhoneNumber(string value)
    {
        var normalized = Regex.Replace(value, @"[\s\-\(\)]", "");
        if (normalized.StartsWith('+'))
            normalized = normalized[1..];
        return normalized.Length >= 7 && normalized.All(char.IsDigit);
    }
}
