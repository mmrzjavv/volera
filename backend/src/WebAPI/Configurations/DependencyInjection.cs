using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MediatR;
using Core.Domain.Interfaces;
using Core.Application.Interfaces;
using Core.Application.Behaviors;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Infrastructure.Security;
using WebAPI.Services;

namespace WebAPI.Configurations;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICallRepository, CallRepository>();

        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator>(sp => new JwtTokenGenerator(sp.GetRequiredService<IConfiguration>()));

        return services;
    }

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Core.Application.Commands.RegisterUserCommand).Assembly));

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddScoped<ICallNotificationService, CallNotificationService>();
        services.AddScoped<IOnlineUserService, OnlineUserService>();

        return services;
    }

    public static IServiceCollection AddWebAPI(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSignalR();

        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", builder =>
            {
                builder.WithOrigins("http://127.0.0.1:5500")
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .AllowCredentials();
            });
        });

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
                ValidIssuer = "VoiceCallApp",
                ValidAudience = "VoiceCallApp",
                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                    System.Text.Encoding.UTF8.GetBytes("mysecretkey1234567890123456789012345678901234567890"))
            };
        });

        services.AddAuthorization();

        return services;
    }
}