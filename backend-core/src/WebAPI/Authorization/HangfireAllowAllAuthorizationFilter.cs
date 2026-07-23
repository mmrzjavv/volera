using Hangfire.Dashboard;

namespace WebAPI.Authorization;

/// <summary>
/// Allows all requests to the Hangfire dashboard. Restrict in production (e.g. by role or local-only).
/// </summary>
public class HangfireAllowAllAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}
