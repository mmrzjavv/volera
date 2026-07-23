namespace Core.Domain.Entities;

/// <summary>
/// User role stored as enum value (int) in the database.
/// </summary>
public enum UserRole
{
    User = 0,
    Moderator = 1,
    Admin = 2,
    SuperAdmin = 3,
    /// <summary>Guest users are synthetic; login must be rejected for this role.</summary>
    Guest = 4,
    /// <summary>Company widget clients; login must be rejected for this role.</summary>
    CompanyClient = 5
}

public static class UserRoleExtensions
{
    public static string ToRoleName(this UserRole role)
    {
        return role switch
        {
            UserRole.User => "User",
            UserRole.Moderator => "Moderator",
            UserRole.Admin => "Admin",
            UserRole.SuperAdmin => "SuperAdmin",
            UserRole.Guest => "Guest",
            UserRole.CompanyClient => "CompanyClient",
            _ => "User"
        };
    }

    public static bool IsAdminRole(this UserRole role) =>
        role == UserRole.Admin || role == UserRole.Moderator || role == UserRole.SuperAdmin;
    public static bool IsGuest(this UserRole role) => role == UserRole.Guest;
    public static bool IsCompanyClient(this UserRole role) => role == UserRole.CompanyClient;

    public static UserRole FromName(string name)
    {
        return name switch
        {
            "User" => UserRole.User,
            "Moderator" => UserRole.Moderator,
            "Admin" => UserRole.Admin,
            "SuperAdmin" => UserRole.SuperAdmin,
            "Guest" => UserRole.Guest,
            "CompanyClient" => UserRole.CompanyClient,
            _ => UserRole.User
        };
    }

    public static UserRole FromValue(int value)
    {
        if (Enum.IsDefined(typeof(UserRole), value))
            return (UserRole)value;
        return UserRole.User;
    }
}
