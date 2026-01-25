using Infrastructure.Persistence;
using Core.Domain.Entities;
using Infrastructure.Security;
using Core.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Configurations;

public static class DatabaseInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        if (!await context.Users.AnyAsync(u => u.Username == "user1"))
        {
            var user1 = new User("John", "Doe", "user1", "+1234567890", passwordHasher.HashPassword("password123"));
            context.Users.Add(user1);
        }

        if (!await context.Users.AnyAsync(u => u.Username == "user2"))
        {
            var user2 = new User("Jane", "Smith", "user2", "+0987654321", passwordHasher.HashPassword("password123"));
            context.Users.Add(user2);
        }

        await context.SaveChangesAsync();
    }
}