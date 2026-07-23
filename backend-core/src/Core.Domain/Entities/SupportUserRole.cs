namespace Core.Domain.Entities;

/// <summary>
/// Role for support users within a company. Separate from UserRole.
/// </summary>
public enum SupportUserRole
{
    SupportAgent = 0,
    SupportManager = 1,
    CompanyAdmin = 2
}

public static class SupportUserRoleExtensions
{
    public static string ToRoleName(this SupportUserRole role)
    {
        return role switch
        {
            SupportUserRole.SupportAgent => "SupportAgent",
            SupportUserRole.SupportManager => "SupportManager",
            SupportUserRole.CompanyAdmin => "CompanyAdmin",
            _ => "SupportAgent"
        };
    }

    public static bool CanManageSupportUsers(this SupportUserRole role) =>
        role == SupportUserRole.SupportManager || role == SupportUserRole.CompanyAdmin;

    public static bool CanManageCompany(this SupportUserRole role) =>
        role == SupportUserRole.CompanyAdmin;

    public static bool CanViewAllCompanyMessages(this SupportUserRole role) =>
        role == SupportUserRole.SupportManager || role == SupportUserRole.CompanyAdmin;
}
