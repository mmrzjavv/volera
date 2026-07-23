namespace WebAPI.Middlewares;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionMiddleware>();
    }

    public static IApplicationBuilder UseAppVersionCheckMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AppVersionCheckMiddleware>();
    }
}