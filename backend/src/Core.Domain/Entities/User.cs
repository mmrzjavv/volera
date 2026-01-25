using System;
using Shared;

namespace Core.Domain.Entities;

public class User : BaseEntity
{
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Username { get; private set; }
    public string PhoneNumber { get; private set; }
    public string PasswordHash { get; private set; }
    public string? ProfilePicture { get; private set; }

    private User() { } // EF Core

    public User(string firstName, string lastName, string username, string phoneNumber, string passwordHash)
    {
        FirstName = firstName;
        LastName = lastName;
        Username = username;
        PhoneNumber = phoneNumber;
        PasswordHash = passwordHash;
    }

    public void UpdateProfile(string firstName, string lastName, string? profilePicture)
    {
        FirstName = firstName;
        LastName = lastName;
        ProfilePicture = profilePicture;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
    }
}