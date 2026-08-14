using Serilog;
using Serilog.Events;

namespace WebAPI.Logging;

public static class SerilogConfiguration
{
    public static void Configure(HostBuilderContext context, IServiceProvider services, LoggerConfiguration configuration)
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", "Volera.WebAPI")
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.Mvc", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.Authorization", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Query", LogEventLevel.Error)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Model.Validation", LogEventLevel.Error)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .MinimumLevel.Override("Hangfire", LogEventLevel.Warning)
            .MinimumLevel.Override("Hangfire.Server", LogEventLevel.Warning)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");

        var seqUrl = context.Configuration["SEQ_URL"]
            ?? context.Configuration["Seq:ServerUrl"]
            ?? context.Configuration["Serilog:WriteTo:0:Args:serverUrl"]
            ?? context.Configuration["Serilog:WriteTo:0:Args:ServerUrl"];

        if (!string.IsNullOrWhiteSpace(seqUrl))
        {
            configuration.WriteTo.Seq(seqUrl);
        }
    }

    public static bool IsHealthOrNoisePath(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        return path.Equals("/health", StringComparison.OrdinalIgnoreCase)
               || path.Equals("/health/ready", StringComparison.OrdinalIgnoreCase)
               || path.Equals("/version", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/hangfire", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase);
    }
}
