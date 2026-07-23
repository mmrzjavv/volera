using Infrastructure.Persistence;
using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Core.Application.Interfaces;
using System.Text.Json;

namespace WebAPI.Configurations;

public static class DatabaseInitializer
{
    private const string DemoSeedMarkerKey = "DemoContentSeeded";
    private const string DemoSeedVersion = "stories-ui-v1";
    private const string SeedPassword = "password123";

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");

        await EnsureBaseUsersAsync(context, passwordHasher);
        await EnsureSystemLimitsAsync(context);
        await EnsureOptionalSeedAdminAsync(context, passwordHasher, config);
        await EnsureAppSettingsAsync(context, config);

        await context.SaveChangesAsync();

        var seedDemo = config.GetValue("SeedDemo:Enabled", true);
        if (seedDemo)
        {
            await EnsureDemoContentAsync(context, logger);
            await context.SaveChangesAsync();
        }
    }

    private static async Task EnsureBaseUsersAsync(ApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        async Task EnsureUser(string username, string first, string last, string phone, UserRole role)
        {
            if (!await context.Users.AnyAsync(u => u.Username == username))
            {
                context.Users.Add(new User(first, last, username, phone, passwordHasher.HashPassword(SeedPassword), role));
            }
        }

        await EnsureUser("user1", "John", "Doe", "+1234567890", UserRole.SuperAdmin);
        await EnsureUser("user2", "Jane", "Smith", "+0987654321", UserRole.User);
        await EnsureUser("admin1", "Admin", "One", "+1111111111", UserRole.Admin);
        await EnsureUser("mod1", "Moderator", "One", "+2222222222", UserRole.Moderator);
        await EnsureUser("user3", "Alex", "Rivera", "+13335550001", UserRole.User);
        await EnsureUser("user4", "Sam", "Lee", "+13335550002", UserRole.User);
    }

    private static async Task EnsureSystemLimitsAsync(ApplicationDbContext context)
    {
        async Task EnsureLimitAsync(string key, decimal value, string description)
        {
            if (!await context.SystemLimits.AnyAsync(s => s.Key == key))
                context.SystemLimits.Add(new SystemLimit(key, value, description));
        }

        await EnsureLimitAsync(LimitKeys.MaxPinnedMessages, 5, "Maximum pinned messages per chat");
        await EnsureLimitAsync(LimitKeys.MaxSavedMessagesSizeBytes, 50 * 1024 * 1024, "Max saved messages storage in bytes (50MB)");
        await EnsureLimitAsync(LimitKeys.MaxSavedMessagesCount, 100, "Max number of saved messages");
        await EnsureLimitAsync(LimitKeys.MaxSessionsPerUser, 4, "Maximum active sessions (devices) per user");
        await EnsureLimitAsync(LimitKeys.MaxGuestMessagesPerMinute, 10, "Max guest messages per minute (rate limit)");
        await EnsureLimitAsync(LimitKeys.MaxGuestSessionsPerIpPerHour, 10, "Max guest session creations per IP per hour");
        await EnsureLimitAsync(LimitKeys.MaxMessageLength, 2000, "Maximum message length in characters");
    }

    private static async Task EnsureOptionalSeedAdminAsync(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration config)
    {
        var seedAdminUser = config["SeedAdmin:Username"];
        var seedAdminPass = config["SeedAdmin:Password"];
        if (string.IsNullOrWhiteSpace(seedAdminUser) || string.IsNullOrWhiteSpace(seedAdminPass))
            return;

        var existingAdmin = await context.Users.FirstOrDefaultAsync(u => u.Username == seedAdminUser);
        if (existingAdmin == null)
        {
            var phone = config["SeedAdmin:PhoneNumber"] ?? "+989000000001";
            context.Users.Add(new User(
                config["SeedAdmin:FirstName"] ?? "Admin",
                config["SeedAdmin:LastName"] ?? "User",
                seedAdminUser,
                phone,
                passwordHasher.HashPassword(seedAdminPass),
                UserRole.SuperAdmin));
        }
        else
        {
            existingAdmin.ChangePassword(passwordHasher.HashPassword(seedAdminPass));
            if (existingAdmin.Role != UserRole.SuperAdmin && existingAdmin.Role != UserRole.Admin)
            {
                var superCount = await context.Users.CountAsync(u => u.Role == UserRole.SuperAdmin);
                existingAdmin.SetRole(UserRole.SuperAdmin, superCount);
            }
        }
    }

    private static async Task EnsureAppSettingsAsync(ApplicationDbContext context, IConfiguration config)
    {
        if (!await context.AppSettings.AnyAsync(a => a.Key == AppSettingKeys.GuestInboxUserId))
        {
            var inboxUser = await context.Users.OrderBy(u => u.CreatedAt).FirstOrDefaultAsync();
            if (inboxUser != null)
                context.AppSettings.Add(new AppSetting(AppSettingKeys.GuestInboxUserId, inboxUser.Id.ToString()));
        }

        if (!await context.AppSettings.AnyAsync(a => a.Key == AppSettingKeys.AppVersion))
        {
            var version = config["AppVersion"] ?? "1.0.0";
            context.AppSettings.Add(new AppSetting(AppSettingKeys.AppVersion, version));
        }
    }

    private static async Task EnsureDemoContentAsync(ApplicationDbContext context, ILogger logger)
    {
        var marker = await context.AppSettings.FirstOrDefaultAsync(a => a.Key == DemoSeedMarkerKey);
        if (marker?.Value == DemoSeedVersion)
            return;

        var user1 = await context.Users.FirstAsync(u => u.Username == "user1");
        var user2 = await context.Users.FirstAsync(u => u.Username == "user2");
        var user3 = await context.Users.FirstAsync(u => u.Username == "user3");
        var user4 = await context.Users.FirstAsync(u => u.Username == "user4");
        var admin1 = await context.Users.FirstAsync(u => u.Username == "admin1");

        user1.UpdateProfile(user1.FirstName, user1.LastName, null, "john@volera.test", "Building Volera - say hi!");
        user2.UpdateProfile(user2.FirstName, user2.LastName, null, "jane@volera.test", "Coffee and chat");
        user3.UpdateProfile(user3.FirstName, user3.LastName, null, null, "Always online for demos");

        await SeedContactsAsync(context, user1, user2, user3, user4);
        await SeedGroupAndMessagesAsync(context, user1, user2, user3);
        await SeedDirectMessagesAsync(context, user1, user2, user3);
        await SeedCallsAsync(context, user1, user2, user3);
        await SeedSystemMessagesAsync(context, admin1);
        await SeedStoriesAsync(context, user1, user2, user3);

        if (marker == null)
            context.AppSettings.Add(new AppSetting(DemoSeedMarkerKey, DemoSeedVersion));
        else
            marker.SetValue(DemoSeedVersion);

        logger.LogInformation(
            "Demo content seeded (version {Version}). Login: user1 / {Password}",
            DemoSeedVersion,
            SeedPassword);
    }

    private static async Task SeedContactsAsync(
        ApplicationDbContext context,
        User user1,
        User user2,
        User user3,
        User user4)
    {
        async Task EnsureContact(Guid ownerId, User contactUser, string nickname)
        {
            var exists = await context.Contacts.AnyAsync(c =>
                c.OwnerUserId == ownerId && c.ContactUserId == contactUser.Id);
            if (exists) return;

            var contact = new Contact(ownerId, contactUser.PhoneNumber, contactUser.Id, nickname);
            contact.UpdateStatus(ContactStatus.Accepted);
            context.Contacts.Add(contact);
        }

        // Mutual contacts so Stories audience + group picker work
        await EnsureContact(user1.Id, user2, "Jane");
        await EnsureContact(user2.Id, user1, "John");
        await EnsureContact(user1.Id, user3, "Alex");
        await EnsureContact(user3.Id, user1, "John");
        await EnsureContact(user2.Id, user3, "Alex");
        await EnsureContact(user3.Id, user2, "Jane");
        await EnsureContact(user1.Id, user4, "Sam");
        await EnsureContact(user4.Id, user1, "John");

        // Phone-only contact (not on Volera) — exercises disabled row in Create Group
        if (!await context.Contacts.AnyAsync(c =>
                c.OwnerUserId == user1.Id && c.ContactPhoneNumber == "+19998887777"))
        {
            var phoneOnly = new Contact(user1.Id, "+19998887777", null, "Mom (not on app)");
            phoneOnly.UpdateStatus(ContactStatus.Accepted);
            context.Contacts.Add(phoneOnly);
        }
    }

    private static async Task SeedGroupAndMessagesAsync(
        ApplicationDbContext context,
        User user1,
        User user2,
        User user3)
    {
        var existing = await context.Groups.FirstOrDefaultAsync(g => g.Name == "Volera Team");
        Group group;
        if (existing == null)
        {
            group = new Group("Volera Team", user1.Id);
            group.UpdateProfile("Volera Team", "Demo group for chats, members, and invites", null);
            group.AddMember(user1.Id, true);
            group.AddMember(user2.Id, false);
            group.AddMember(user3.Id, false);
            context.Groups.Add(group);
            await context.SaveChangesAsync();
        }
        else
        {
            group = existing;
        }

        if (!await context.Messages.AnyAsync(m => m.GroupId == group.Id))
        {
            context.Messages.Add(new Message(user1.Id, group.Id, "Welcome to Volera Team — try stories & group chat.", true));
            context.Messages.Add(new Message(user2.Id, group.Id, "Looks great! Checking the member list.", true));
            context.Messages.Add(new Message(user3.Id, group.Id, "Mobile layout feels clean.", true));
        }
    }

    private static async Task SeedDirectMessagesAsync(
        ApplicationDbContext context,
        User user1,
        User user2,
        User user3)
    {
        var hasDm = await context.Messages.AnyAsync(m =>
            m.GroupId == null &&
            ((m.SenderId == user1.Id && m.ReceiverId == user2.Id) ||
             (m.SenderId == user2.Id && m.ReceiverId == user1.Id)));

        if (!hasDm)
        {
            context.Messages.Add(new Message(user1.Id, user2.Id, "Hey Jane — demo DM for the Chats tab."));
            context.Messages.Add(new Message(user2.Id, user1.Id, "Hi John! Reply works too."));
            context.Messages.Add(new Message(user1.Id, user2.Id, "Open Stories at the top of Chats when you have a minute."));
        }

        var hasDm3 = await context.Messages.AnyAsync(m =>
            m.GroupId == null &&
            ((m.SenderId == user1.Id && m.ReceiverId == user3.Id) ||
             (m.SenderId == user3.Id && m.ReceiverId == user1.Id)));

        if (!hasDm3)
        {
            context.Messages.Add(new Message(user3.Id, user1.Id, "Ping from Alex — contacts + recent chats seed."));
            context.Messages.Add(new Message(user1.Id, user3.Id, "Got it. Group invite is under Group Info."));
        }
    }

    private static async Task SeedCallsAsync(
        ApplicationDbContext context,
        User user1,
        User user2,
        User user3)
    {
        if (await context.Calls.AnyAsync())
            return;

        var ended = new Call(user1.Id, user2.Id, isVideo: false);
        ended.Accept();
        ended.End();
        context.Calls.Add(ended);

        var missed = new Call(user2.Id, user1.Id, isVideo: true);
        missed.MarkAsMissed();
        context.Calls.Add(missed);

        var rejected = new Call(user3.Id, user1.Id, isVideo: false);
        rejected.Reject();
        context.Calls.Add(rejected);
    }

    private static async Task SeedSystemMessagesAsync(ApplicationDbContext context, User author)
    {
        if (await context.SystemMessages.AnyAsync(m => m.Title == "Welcome to Volera"))
            return;

        context.SystemMessages.Add(new SystemMessage(
            "Welcome to Volera",
            "Demo environment is ready. Explore Chats, Contacts, Groups, Calls, Stories, and Profile.",
            author.Id,
            DateTime.UtcNow.AddDays(30)));

        context.SystemMessages.Add(new SystemMessage(
            "How to test Stories",
            "Log in as user1 / password123. Open Chats — the story ring strip is at the top. Tap + to post, or open Jane/Alex rings.",
            author.Id,
            DateTime.UtcNow.AddDays(30)));
    }

    private static async Task SeedStoriesAsync(
        ApplicationDbContext context,
        User user1,
        User user2,
        User user3)
    {
        if (await context.Stories.AnyAsync(s => s.DeletedAt == null && s.ExpiresAt > DateTime.UtcNow))
            return;

        static string Overlay(string text, string color = "#ffffff") =>
            JsonSerializer.Serialize(new { text, color, x = 0.5, y = 0.72, fontScale = 1.0 });

        // Public placeholder images (ResolveClientUrl passes http(s) through)
        var story1 = new Story(user1.Id, DateTime.UtcNow.AddHours(23));
        story1.AddItem("Image", "https://picsum.photos/seed/volera-john/720/1280", 5000, Overlay("Hello from John"), 0);
        story1.AddItem("Image", "https://picsum.photos/seed/volera-john-2/720/1280", 5000, Overlay("Multi-item story", "#fef08a"), 1);
        context.Stories.Add(story1);

        var story2 = new Story(user2.Id, DateTime.UtcNow.AddHours(20));
        story2.AddItem("Image", "https://picsum.photos/seed/volera-jane/720/1280", 5000, Overlay("Jane's story"), 0);
        context.Stories.Add(story2);

        var story3 = new Story(user3.Id, DateTime.UtcNow.AddHours(18));
        story3.AddItem("Image", "https://picsum.photos/seed/volera-alex/720/1280", 5000, Overlay("Alex here", "#67e8f9"), 0);
        context.Stories.Add(story3);
    }
}
