using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MediatR;
using FluentValidation;
using AutoMapper;
using Core.Domain.Interfaces;
using Core.Application.Interfaces;
using Core.Application.Behaviors;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Infrastructure.Security;
using Infrastructure.Services;
using WebAPI.Services;
using StackExchange.Redis;
using Hangfire;
using Hangfire.PostgreSql;

namespace WebAPI.Configurations;

    public static class DependencyInjection
    {
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseNpgsql(connectionString, npgsqlOptions =>
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null));
        });

        // Redis connection (used for distributed presence/online user tracking and AI widget queue/session)
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConnectionString));
            services.AddScoped<IOnlineUserService, RedisOnlineUserService>();
            services.AddScoped<ISessionCache, RedisSessionCache>();
            services.AddSingleton<Core.Application.Interfaces.IAiJobQueue, Infrastructure.Services.RedisAiJobQueue>();
            services.AddSingleton<Core.Application.Interfaces.IAiWidgetSessionService, Infrastructure.Services.RedisAiWidgetSessionService>();
        }
        else
        {
            services.AddScoped<IOnlineUserService, WebAPI.Services.OnlineUserService>();
            services.AddScoped<ISessionCache, NullSessionCache>();
            services.AddSingleton<Core.Application.Interfaces.IAiJobQueue, WebAPI.Services.InMemoryAiJobQueue>();
            services.AddSingleton<Core.Application.Interfaces.IAiWidgetSessionService, WebAPI.Services.InMemoryAiWidgetSessionService>();
        }

        services.AddScoped<ISessionService, SessionService>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICallRepository, CallRepository>();
        services.AddScoped<IGroupCallRepository, GroupCallRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IContactRepository, ContactRepository>();
        services.AddScoped<IStoryRepository, StoryRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<ISuggestedPostRepository, SuggestedPostRepository>();
        services.AddScoped<IMessageViewRepository, MessageViewRepository>();
        services.AddScoped<IPushSubscriptionRepository, PushSubscriptionRepository>();
        services.AddScoped<ISavedMessageRepository, SavedMessageRepository>();
        services.AddScoped<IMessageReactionRepository, MessageReactionRepository>();
        services.AddScoped<ISystemMessageRepository, SystemMessageRepository>();
        services.AddScoped<ISystemMessageReadRepository, SystemMessageReadRepository>();
        services.AddScoped<IMessageReadModelService, MessageReadModelService>();
        services.AddScoped<IAdminAuditLogRepository, AdminAuditLogRepository>();
        services.AddScoped<ISystemLimitRepository, SystemLimitRepository>();
        services.AddScoped<IUserLimitOverrideRepository, UserLimitOverrideRepository>();
        services.AddScoped<IAppSettingRepository, AppSettingRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IGuestRepository, GuestRepository>();
        services.AddScoped<IHiddenChatRepository, HiddenChatRepository>();
        services.AddScoped<IAdminReadRepository, AdminReadRepository>();
        services.AddScoped<ILimitResolutionService, LimitResolutionService>();
        services.AddSingleton<IConnectionManager, ConnectionManager>();

        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<ISupportUserRepository, SupportUserRepository>();
        services.AddScoped<ISupportUserBranchRepository, SupportUserBranchRepository>();
        services.AddScoped<ICompanyWidgetRepository, CompanyWidgetRepository>();
        services.AddScoped<ICompanyClientRepository, CompanyClientRepository>();
        services.AddScoped<ICompanyAiWidgetRepository, CompanyAiWidgetRepository>();
        services.AddScoped<IAiContentBlockRepository, AiContentBlockRepository>();

        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator>(sp => new JwtTokenGenerator(sp.GetRequiredService<IConfiguration>()));
        services.AddScoped<IRefreshTokenHasher, RefreshTokenHasher>();
        services.AddScoped<IGuestTokenService, GuestTokenService>();
        services.AddScoped<ICompanyTokenService, CompanyTokenService>();
        services.AddScoped<ICompanyWidgetTokenService, CompanyWidgetTokenService>();
        services.AddSingleton<ISupportUserJwtTokenGenerator>(sp => new SupportUserJwtTokenGenerator(sp.GetRequiredService<IConfiguration>()));

        // Soft-fail storage: text messaging boots without Liara/S3 configured.
        var storageAccessKey = configuration["Storage:AccessKey"];
        var storageSecretKey = configuration["Storage:SecretKey"];
        var storageBucket = configuration["Storage:BucketName"];
        var storageEndpoint = configuration["Storage:EndpointUrl"];
        if (!string.IsNullOrWhiteSpace(storageAccessKey)
            && !string.IsNullOrWhiteSpace(storageSecretKey)
            && !string.IsNullOrWhiteSpace(storageBucket)
            && !string.IsNullOrWhiteSpace(storageEndpoint))
        {
            services.AddScoped<IPublicStorageEndpointProvider, RequestHostPublicStorageEndpointProvider>();
            services.AddScoped<IFileStorageService, LiaraStorageService>();
        }
        else
        {
            services.AddScoped<IFileStorageService, NullFileStorageService>();
        }

        return services;
    }

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Core.Application.Commands.RegisterUserCommand).Assembly));

        // AutoMapper profiles from Core.Application (e.g. ApplicationMappingProfile)
        services.AddAutoMapper(typeof(Core.Application.Mapping.ApplicationMappingProfile).Assembly);

        // Register all FluentValidation validators from the Core.Application assembly
        services.AddValidatorsFromAssemblyContaining<Core.Application.Commands.RegisterUserCommand>();

        // Pipeline behaviors: validation then logging to capture a story for each request
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        services.AddScoped<ICallNotificationService, CallNotificationService>();
        services.AddScoped<IGroupCallNotificationService, GroupCallNotificationService>();
        services.AddScoped<IMessageNotificationService, ChatNotificationService>();
        services.AddScoped<IStoryNotificationService, StoryNotificationService>();
        // Online user service is provided from Infrastructure; if Redis is not configured,
        // OnlineUserService can be registered separately for local/dev scenarios.

        return services;
    }

    public static IServiceCollection AddWebAPI(this IServiceCollection services, IConfiguration configuration)
    {
        // Allow services (like CurrentUserService) to access HttpContext
        services.AddHttpContextAccessor();

        services.AddScoped<IPushNotificationService, PushNotificationService>();
        services.AddScoped<IAppVersionService, AppVersionService>();
        services.AddHostedService<OutboxProcessorHostedService>();

        // Current user abstraction for use in handlers and services
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IRequestContextService, RequestContextService>();
        services.AddSingleton<IGuestConnectionManager, GuestConnectionManager>();
        services.AddSingleton<ICompanyClientConnectionManager, CompanyClientConnectionManager>();


        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
            options.MaximumReceiveMessageSize = 102400;
        });

        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", builder =>
            {
                builder.SetIsOriginAllowed(origin => true) // Allow any origin for development
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .AllowCredentials();
            });
        });

        var jwtKey = JwtConfiguration.RequireSigningKey(configuration, "Jwt:Key");
        var jwtIssuer = configuration["Jwt:Issuer"] ?? "Volera";
        var jwtAudience = configuration["Jwt:Audience"] ?? "Volera";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = "Bearer";
            options.DefaultChallengeScheme = "Bearer";
        }).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                    System.Text.Encoding.UTF8.GetBytes(jwtKey))
            };
            options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    var principal = context.Principal;
                    var userId = principal?.FindFirst("userId")?.Value;
                    if (userId != null && principal?.Identity is System.Security.Claims.ClaimsIdentity identity)
                    {
                        identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId));
                    }
                    return Task.CompletedTask;
                },
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        })
        .AddJwtBearer("SupportUser", options =>
        {
            var supportKey = JwtConfiguration.RequireSigningKey(configuration, "Jwt:SupportUser:Key", "Jwt:Key");
            var supportIssuer = configuration["Jwt:SupportUser:Issuer"] ?? configuration["Jwt:Issuer"] ?? "Volera-Support";
            var supportAudience = configuration["Jwt:SupportUser:Audience"] ?? configuration["Jwt:Audience"] ?? "Volera-Support";
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = supportIssuer,
                ValidAudience = supportAudience,
                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                    System.Text.Encoding.UTF8.GetBytes(supportKey))
            };
            options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(accessToken))
                        context.Token = accessToken;
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("Admin", policy =>
                policy.RequireRole("Admin", "Moderator", "SuperAdmin"));
            options.AddPolicy("CompanyAdmin", policy =>
                policy.RequireRole("CompanyAdmin").AddAuthenticationSchemes("SupportUser"));
            options.AddPolicy("SupportManager", policy =>
                policy.RequireRole("SupportManager", "CompanyAdmin").AddAuthenticationSchemes("SupportUser"));
            options.AddPolicy("SupportAgent", policy =>
                policy.RequireRole("SupportAgent", "SupportManager", "CompanyAdmin").AddAuthenticationSchemes("SupportUser"));
        });

        // WebRTC ICE servers — Coturn (Docker) by default; optional explicit STUN/TURN URLs
        services.Configure<WebAPI.Options.WebRtcOptions>(options =>
        {
            configuration.GetSection(WebAPI.Options.WebRtcOptions.SectionName).Bind(options);

            var coturnEnabled = configuration["COTURN_ENABLED"] ?? configuration["WebRtc:CoturnEnabled"];
            if (!string.IsNullOrWhiteSpace(coturnEnabled) && bool.TryParse(coturnEnabled, out var enabled))
                options.CoturnEnabled = enabled;

            var publicHost = configuration["TURN_PUBLIC_HOST"] ?? configuration["WebRtc:PublicHost"];
            if (!string.IsNullOrWhiteSpace(publicHost))
                options.PublicHost = publicHost.Trim();

            var portStr = configuration["TURN_PORT"] ?? configuration["WebRtc:Port"];
            if (!string.IsNullOrWhiteSpace(portStr) && int.TryParse(portStr, out var port) && port > 0)
                options.Port = port;

            // Flat env aliases (STUN_SERVER_URL / TURN_SERVER_URL); comma-separated allowed
            var stun = configuration["STUN_SERVER_URL"];
            if (!string.IsNullOrWhiteSpace(stun) && (options.StunUrls == null || options.StunUrls.Length == 0))
                options.StunUrls = stun.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var turn = configuration["TURN_SERVER_URL"];
            if (!string.IsNullOrWhiteSpace(turn) && (options.TurnUrls == null || options.TurnUrls.Length == 0))
                options.TurnUrls = turn.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var turnUser = configuration["TURN_USERNAME"];
            if (!string.IsNullOrWhiteSpace(turnUser) && string.IsNullOrWhiteSpace(options.TurnUsername))
                options.TurnUsername = turnUser;

            var turnCred = configuration["TURN_CREDENTIAL"];
            if (!string.IsNullOrWhiteSpace(turnCred) && string.IsNullOrWhiteSpace(options.TurnCredential))
                options.TurnCredential = turnCred;
        });

        // AI Widget: HTTP client for Python service; job processing via Hangfire
        services.Configure<WebAPI.Services.AiServiceClientOptions>(
            configuration.GetSection(WebAPI.Services.AiServiceClientOptions.SectionName));
        services.AddHttpClient<WebAPI.Services.AiServiceClient>();
        services.AddScoped<Core.Application.Interfaces.IAiServiceClient>(sp => sp.GetRequiredService<WebAPI.Services.AiServiceClient>());
        services.AddScoped<IAiJobEnqueuer, HangfireAiJobEnqueuer>();
        services.AddScoped<AiIngestJob>();
        services.AddScoped<AiChatJob>();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));
        services.AddHangfireServer();

        return services;
    }
}