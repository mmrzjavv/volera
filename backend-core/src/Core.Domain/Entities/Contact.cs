using System;
using Shared;

namespace Core.Domain.Entities;

public class Contact : BaseEntity
{
    public Guid OwnerUserId { get; private set; }
    public User OwnerUser { get; private set; }
    public Guid? ContactUserId { get; private set; }
    public User? ContactUser { get; private set; }
    public string? ContactName { get; private set; }
    public string ContactPhoneNumber { get; private set; }
    public ContactStatus Status { get; private set; }

    private Contact() { } // EF Core

    public Contact(Guid ownerUserId, string contactPhoneNumber, Guid? contactUserId = null, string? contactName = null)
    {
        OwnerUserId = ownerUserId;
        ContactPhoneNumber = contactPhoneNumber;
        ContactUserId = contactUserId;
        ContactName = contactName;
        Status = ContactStatus.Pending;
    }

    public void UpdateStatus(ContactStatus status)
    {
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateName(string name)
    {
        ContactName = name;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum ContactStatus
{
    Pending,
    Accepted,
    Rejected,
    Blocked
}
