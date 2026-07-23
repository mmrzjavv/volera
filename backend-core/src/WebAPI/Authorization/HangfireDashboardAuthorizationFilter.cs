using Hangfire.Dashboard;

namespace WebAPI.Authorization;

/// <summary>
/// Hangfire dashboard: open in Development only; otherwise require authenticated Admin/SuperAdmin/Moderator.
/// </summary>
public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly IHostEnvironment _environment;

    public HangfireDashboardAuthorizationFilter(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public bool Authorize(DashboardContext context)
    {
        if (_environment.IsDevelopment())
            return true;

        var http = context.GetHttpContext();
        var user = http.User;
        if (user.Identity?.IsAuthenticated != true)
            return false;

        return user.IsInRole("Admin")
            || user.IsInRole("SuperAdmin")
            || user.IsInRole("Moderator");
    }
}
