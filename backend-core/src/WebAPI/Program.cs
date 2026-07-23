using System.Text.Json.Serialization;
using Hangfire;
using WebAPI.Authorization;
using WebAPI.Configurations;
using WebAPI.Hubs;
using WebAPI.Middlewares;
using Infrastructure.Persistence;
using Core.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .WriteTo.Console();

    var seqUrl = context.Configuration["SEQ_URL"]
        ?? context.Configuration["Seq:ServerUrl"]
        ?? context.Configuration["Serilog:WriteTo:0:Args:ServerUrl"];

    if (!string.IsNullOrWhiteSpace(seqUrl))
    {
        configuration.WriteTo.Seq(seqUrl);
    }
});

// Host-only local Docker DB wiring. Do NOT load inside containers — env vars from compose win.
if (builder.Environment.IsDevelopment()
    && string.Equals(Environment.GetEnvironmentVariable("USE_DOCKER_LOCAL"), "true", StringComparison.OrdinalIgnoreCase))
{
    builder.Configuration.AddJsonFile("appsettings.DockerLocal.json", optional: true, reloadOnChange: true);
}

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("GuestCreateSession", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0
            }));
    options.AddPolicy("CompanyWidgetClientSession", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0
            }));
    options.AddPolicy("AuthLogin", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0
            }));
    options.AddPolicy("MessageSend", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.Identity?.Name
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.AddPolicy("AuthenticatedUploads", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.Identity?.Name
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 40,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0
            }));
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddWebAPI(builder.Configuration);

var app = builder.Build();

// Apply migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        if (context.Database.IsRelational())
        {
            context.Database.Migrate();
        }
        await DatabaseInitializer.SeedAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}



app.UseGlobalExceptionMiddleware();

app.UseCors("AllowFrontend");
app.UseStaticFiles();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAppVersionCheckMiddleware();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireDashboardAuthorizationFilter(app.Environment) }
});

Hangfire.RecurringJob.AddOrUpdate<WebAPI.Jobs.StoryExpiryJob>(
    "story-expiry",
    job => job.Process(),
    Hangfire.Cron.Hourly);

app.MapControllers();
app.MapHub<CallHub>("/callHub");
app.MapHub<ChatHub>("/chatHub");
app.MapHub<GuestHub>("/guestHub");
app.MapHub<CompanyWidgetHub>("/companyWidgetHub");
app.MapHub<SupportHub>("/supportHub");
app.MapHub<WebAPI.Hubs.AiWidgetHub>("/aiWidgetHub");

app.MapGet("/health", () => Results.Ok(new { status = "OK" }));

app.MapGet("/health/ready", (IFileStorageService storage, IConfiguration config) =>
{
    var hasDb = !string.IsNullOrWhiteSpace(config.GetConnectionString("DefaultConnection"));
    var hasJwt = !string.IsNullOrWhiteSpace(config["Jwt:Key"]);
    var ready = hasDb && hasJwt;
    return Results.Json(new
    {
        status = ready ? "Ready" : "Degraded",
        databaseConfigured = hasDb,
        jwtConfigured = hasJwt,
        storageConfigured = storage.IsConfigured
    }, statusCode: ready ? 200 : 503);
});

app.MapGet("/version", async (WebAPI.Services.IAppVersionService versionService) =>
{
    var version = await versionService.GetVersionAsync();
    return Results.Json(new { version });
});

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
