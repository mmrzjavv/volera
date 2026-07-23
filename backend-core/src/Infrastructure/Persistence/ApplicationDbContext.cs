using Microsoft.EntityFrameworkCore;
using Core.Domain.Entities;
using Shared;

namespace Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Call> Calls { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    public DbSet<Contact> Contacts { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<GroupMember> GroupMembers { get; set; }
    public DbSet<GroupCall> GroupCalls { get; set; }
    public DbSet<GroupCallParticipant> GroupCallParticipants { get; set; }
    public DbSet<MessageView> MessageViews { get; set; }
    public DbSet<SuggestedPost> SuggestedPosts { get; set; }
    public DbSet<SystemMessage> SystemMessages { get; set; }
    public DbSet<SystemMessageRead> SystemMessageReads { get; set; }
    public DbSet<PushSubscription> PushSubscriptions { get; set; }
    public DbSet<SavedMessage> SavedMessages { get; set; }
    public DbSet<MessageReaction> MessageReactions { get; set; }
    public DbSet<AdminAuditLog> AdminAuditLogs { get; set; }
    public DbSet<SystemLimit> SystemLimits { get; set; }
    public DbSet<UserLimitOverride> UserLimitOverrides { get; set; }
    public DbSet<AppSetting> AppSettings { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<Guest> Guests { get; set; }
    public DbSet<HiddenChat> HiddenChats { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<Branch> Branches { get; set; }
    public DbSet<SupportUser> SupportUsers { get; set; }
    public DbSet<SupportUserBranch> SupportUserBranches { get; set; }
    public DbSet<CompanyWidget> CompanyWidgets { get; set; }
    public DbSet<CompanyClient> CompanyClients { get; set; }
    public DbSet<CompanyAiWidget> CompanyAiWidgets { get; set; }
    public DbSet<AiContentBlock> AiContentBlocks { get; set; }
    public DbSet<Story> Stories { get; set; }
    public DbSet<StoryItem> StoryItems { get; set; }
    public DbSet<StoryView> StoryViews { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SavedMessage
        modelBuilder.Entity<SavedMessage>(entity =>
        {
            entity.HasKey(sm => sm.Id);
            entity.HasOne(sm => sm.User).WithMany().HasForeignKey(sm => sm.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(sm => sm.Message).WithMany().HasForeignKey(sm => sm.MessageId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(sm => new { sm.UserId, sm.MessageId }).IsUnique();
        });

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.FirstName).IsRequired().HasMaxLength(50);
            entity.Property(u => u.LastName).IsRequired().HasMaxLength(50);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.Username).IsRequired().HasMaxLength(20);
            entity.HasIndex(u => u.PhoneNumber).IsUnique();
            entity.Property(u => u.PhoneNumber).IsRequired().HasMaxLength(15);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.ProfilePicture).HasMaxLength(500);
        });

        // AdminAuditLog
        modelBuilder.Entity<AdminAuditLog>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Action).IsRequired().HasMaxLength(100);
            entity.Property(a => a.ResourceType).IsRequired().HasMaxLength(50);
            entity.Property(a => a.Details).HasMaxLength(2000);
            entity.HasIndex(a => new { a.AdminUserId, a.CreatedAt });
            entity.HasIndex(a => new { a.ResourceType, a.ResourceId });
        });

        // SystemLimit
        modelBuilder.Entity<SystemLimit>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Key).IsRequired().HasMaxLength(100);
            entity.HasIndex(s => s.Key).IsUnique();
            entity.Property(s => s.Description).HasMaxLength(500);
        });

        // UserLimitOverride
        modelBuilder.Entity<UserLimitOverride>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.LimitKey).IsRequired().HasMaxLength(100);
            entity.HasIndex(u => new { u.UserId, u.LimitKey }).IsUnique();
            entity.HasOne(u => u.User).WithMany().HasForeignKey(u => u.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // AppSetting
        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Key).IsRequired().HasMaxLength(100);
            entity.Property(a => a.Value).IsRequired().HasMaxLength(500);
            entity.HasIndex(a => a.Key).IsUnique();
        });

        // Session
        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.DeviceType).IsRequired().HasMaxLength(50);
            entity.Property(s => s.Browser).IsRequired().HasMaxLength(100);
            entity.Property(s => s.OS).IsRequired().HasMaxLength(100);
            entity.Property(s => s.Location).IsRequired().HasMaxLength(200);
            entity.Property(s => s.AppVersion).IsRequired().HasMaxLength(20);
            entity.Property(s => s.RefreshTokenHash).HasMaxLength(64);
            entity.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(s => new { s.UserId, s.RevokedAt });
            entity.HasIndex(s => new { s.UserId, s.LastActivityAt });
            entity.HasIndex(s => s.RefreshTokenHash).IsUnique();
        });

        // Contact
        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.ContactName).HasMaxLength(100);
            entity.Property(c => c.ContactPhoneNumber).IsRequired().HasMaxLength(15);
            entity.Property(c => c.Status).IsRequired();

            entity.HasOne(c => c.OwnerUser)
                  .WithMany()
                  .HasForeignKey(c => c.OwnerUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.ContactUser)
                  .WithMany()
                  .HasForeignKey(c => c.ContactUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Call
        modelBuilder.Entity<Call>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Status).IsRequired();
            entity.HasOne(c => c.Caller).WithMany().HasForeignKey(c => c.CallerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(c => c.Receiver).WithMany().HasForeignKey(c => c.ReceiverId).OnDelete(DeleteBehavior.Restrict);
        });

        // Message
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Content).IsRequired().HasMaxLength(2000);
            entity.HasOne(m => m.Sender).WithMany().HasForeignKey(m => m.SenderId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(m => m.Receiver).WithMany().HasForeignKey(m => m.ReceiverId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(m => m.SupportSender).WithMany().HasForeignKey(m => m.SupportSenderId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
            entity.HasOne<User>().WithMany().HasForeignKey(m => m.TargetReceiverUserId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
            entity.HasOne(m => m.Group).WithMany(g => g.Messages).HasForeignKey(m => m.GroupId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(m => m.SendAsChannel).WithMany().HasForeignKey(m => m.SendAsChannelId).OnDelete(DeleteBehavior.SetNull).IsRequired(false);
            entity.Property(m => m.SignatureDisplayName).HasMaxLength(200);

            // Self-referencing reply relationship
            entity.HasOne(m => m.ReplyToMessage)
                  .WithMany()
                  .HasForeignKey(m => m.ReplyToMessageId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Indices for performance
            entity.HasIndex(m => m.SentAt);
            entity.HasIndex(m => new { m.SenderId, m.ReceiverId });
            entity.HasIndex(m => new { m.GroupId, m.SentAt });
            entity.HasIndex(m => new { m.SenderId, m.ReceiverId, m.IsPinned });
            entity.HasIndex(m => new { m.GroupId, m.IsPinned });
            entity.Property(m => m.BranchId).IsRequired(false);
            entity.Property(m => m.CompanyId).IsRequired(false);
            entity.HasIndex(m => new { m.BranchId, m.SentAt });
            entity.HasIndex(m => new { m.CompanyId, m.BranchId });
            entity.Property(m => m.ClientMessageId).IsRequired(false);
            entity.HasIndex(m => new { m.SenderId, m.ClientMessageId })
                .IsUnique()
                .HasFilter("\"ClientMessageId\" IS NOT NULL");

            entity.HasOne(m => m.ReplyToStoryItem)
                  .WithMany()
                  .HasForeignKey(m => m.ReplyToStoryItemId)
                  .OnDelete(DeleteBehavior.SetNull)
                  .IsRequired(false);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Type).IsRequired().HasMaxLength(100);
            entity.Property(o => o.Payload).IsRequired();
            entity.Property(o => o.LastError).HasMaxLength(2000);
            entity.HasIndex(o => new { o.Status, o.NextAttemptAt });
            entity.HasIndex(o => o.CreatedAt);
        });

        // MessageReaction
        modelBuilder.Entity<MessageReaction>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Emoji).IsRequired().HasMaxLength(16);
            entity.HasIndex(r => new { r.MessageId, r.UserId }).IsUnique().HasFilter("\"UserId\" IS NOT NULL");
            entity.HasIndex(r => new { r.MessageId, r.SupportUserId }).IsUnique().HasFilter("\"SupportUserId\" IS NOT NULL");

            entity.HasOne(r => r.Message)
                  .WithMany(m => m.MessageReactions)
                  .HasForeignKey(r => r.MessageId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.User)
                  .WithMany()
                  .HasForeignKey(r => r.UserId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .IsRequired(false);

            entity.HasOne(r => r.SupportUser)
                  .WithMany()
                  .HasForeignKey(r => r.SupportUserId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .IsRequired(false);
        });

        // Group
        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Name).IsRequired().HasMaxLength(100);
            entity.Property(g => g.Description).HasMaxLength(500);
            entity.Property(g => g.ProfilePictureUrl).HasMaxLength(500);
            entity.Property(g => g.InviteCode).HasMaxLength(64);
            entity.Property(g => g.PublicUsername).HasMaxLength(64);
            entity.Property(g => g.Kind).HasConversion<int>();
            entity.HasOne(g => g.Admin).WithMany().HasForeignKey(g => g.AdminId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(g => g.LinkedDiscussionGroup)
                  .WithMany()
                  .HasForeignKey(g => g.LinkedDiscussionGroupId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(g => g.PublicUsername)
                  .IsUnique()
                  .HasFilter("\"PublicUsername\" IS NOT NULL");
            entity.HasIndex(g => new { g.Kind, g.IsPublic });
        });

        // GroupMember
        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.HasKey(gm => gm.Id);
            entity.HasIndex(gm => new { gm.GroupId, gm.UserId }).IsUnique();

            entity.HasOne(gm => gm.Group)
                  .WithMany(g => g.Members)
                  .HasForeignKey(gm => gm.GroupId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(gm => gm.User)
                  .WithMany()
                  .HasForeignKey(gm => gm.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MessageView>(entity =>
        {
            entity.HasKey(v => v.Id);
            entity.HasIndex(v => new { v.MessageId, v.UserId }).IsUnique();
            entity.HasOne(v => v.Message)
                  .WithMany(m => m.MessageViews)
                  .HasForeignKey(v => v.MessageId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(v => v.User)
                  .WithMany()
                  .HasForeignKey(v => v.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SuggestedPost>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Content).IsRequired().HasMaxLength(4000);
            entity.Property(s => s.AttachmentUrl).HasMaxLength(1000);
            entity.Property(s => s.AttachmentType).HasMaxLength(100);
            entity.Property(s => s.AdminNote).HasMaxLength(1000);
            entity.Property(s => s.Status).HasConversion<int>();
            entity.HasIndex(s => new { s.ChannelId, s.Status });
            entity.HasOne(s => s.Channel)
                  .WithMany()
                  .HasForeignKey(s => s.ChannelId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(s => s.FromUser)
                  .WithMany()
                  .HasForeignKey(s => s.FromUserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // GroupCall
        modelBuilder.Entity<GroupCall>(entity =>
        {
            entity.HasKey(gc => gc.Id);
            entity.Property(gc => gc.Status).IsRequired();

            entity.HasOne(gc => gc.Group)
                  .WithMany()
                  .HasForeignKey(gc => gc.GroupId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(gc => gc.Initiator)
                  .WithMany()
                  .HasForeignKey(gc => gc.InitiatorId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(gc => new { gc.GroupId, gc.Status });
            entity.HasIndex(gc => gc.StartTime);
        });

        // GroupCallParticipant
        modelBuilder.Entity<GroupCallParticipant>(entity =>
        {
            entity.HasKey(gcp => gcp.Id);
            entity.HasIndex(gcp => new { gcp.GroupCallId, gcp.UserId }).IsUnique();

            entity.HasOne(gcp => gcp.GroupCall)
                  .WithMany(gc => gc.Participants)
                  .HasForeignKey(gcp => gcp.GroupCallId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(gcp => gcp.User)
                  .WithMany()
                  .HasForeignKey(gcp => gcp.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // SystemMessage
        modelBuilder.Entity<SystemMessage>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Title).IsRequired().HasMaxLength(255);
            entity.Property(m => m.Content).IsRequired();
            entity.HasOne(m => m.Author)
                  .WithMany()
                  .HasForeignKey(m => m.AuthorId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // SystemMessageRead
        modelBuilder.Entity<SystemMessageRead>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => new { r.MessageId, r.UserId }).IsUnique();
            entity.HasOne(r => r.Message)
                  .WithMany()
                  .HasForeignKey(r => r.MessageId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(r => r.User)
                  .WithMany()
                  .HasForeignKey(r => r.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // PushSubscription
        modelBuilder.Entity<PushSubscription>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Endpoint).IsRequired();
            entity.Property(p => p.P256dh).IsRequired();
            entity.Property(p => p.Auth).IsRequired();

            // A user can have multiple subscriptions (multiple devices), 
            // but endpoint must be unique per user-device combo
            entity.HasIndex(p => p.Endpoint).IsUnique();

            entity.HasOne(p => p.User)
                  .WithMany()
                  .HasForeignKey(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // HiddenChat: user-hide direct chats from recent list
        modelBuilder.Entity<HiddenChat>(entity =>
        {
            entity.HasKey(h => h.Id);
            entity.HasIndex(h => new { h.UserId, h.OtherUserId }).IsUnique();
            entity.HasOne(h => h.User).WithMany().HasForeignKey(h => h.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.Property(h => h.OtherUserId).IsRequired();
        });

        // Guest: aggregate root for guest chat; linked User exists only for Message.SenderId
        modelBuilder.Entity<Guest>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Property(g => g.FirstName).HasMaxLength(50);
            entity.Property(g => g.LastName).HasMaxLength(50);
            entity.Property(g => g.Email).HasMaxLength(255);
            entity.Property(g => g.Mobile).HasMaxLength(15);
            entity.Property(g => g.SessionTokenHash).IsRequired().HasMaxLength(64);
            entity.HasOne(g => g.User).WithMany().HasForeignKey(g => g.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(g => g.UserId).IsUnique();
            entity.HasIndex(g => g.SessionTokenHash);
            entity.HasIndex(g => g.CreatedAt);
        });

        // Company
        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(200);
            entity.Property(c => c.MobileNumber).IsRequired().HasMaxLength(15);
            entity.HasIndex(c => c.MobileNumber).IsUnique();
            entity.Property(c => c.Email).HasMaxLength(255);
            entity.Property(c => c.Address).HasMaxLength(500);
            entity.Property(c => c.LogoUrl).HasMaxLength(500);
            entity.Property(c => c.RegistrationTokenHash).HasMaxLength(64);
        });

        // Branch
        modelBuilder.Entity<Branch>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Name).IsRequired().HasMaxLength(200);
            entity.Property(b => b.Address).HasMaxLength(500);
            entity.Property(b => b.PhoneNumber).HasMaxLength(15);
            entity.Property(b => b.Email).HasMaxLength(255);
            entity.HasOne(b => b.Company).WithMany().HasForeignKey(b => b.CompanyId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(b => new { b.CompanyId, b.Name });
        });

        // SupportUser
        modelBuilder.Entity<SupportUser>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Username).IsRequired().HasMaxLength(50);
            entity.Property(s => s.PasswordHash).IsRequired();
            entity.Property(s => s.FirstName).IsRequired().HasMaxLength(50);
            entity.Property(s => s.LastName).IsRequired().HasMaxLength(50);
            entity.Property(s => s.Email).HasMaxLength(255);
            entity.Property(s => s.PhoneNumber).HasMaxLength(15);
            entity.Property(s => s.Role).IsRequired().HasConversion<int>();
            entity.HasOne(s => s.Company).WithMany().HasForeignKey(s => s.CompanyId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(s => new { s.CompanyId, s.Username }).IsUnique();
        });

        // SupportUserBranch
        modelBuilder.Entity<SupportUserBranch>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => new { s.SupportUserId, s.BranchId }).IsUnique();
            entity.HasOne(s => s.SupportUser).WithMany().HasForeignKey(s => s.SupportUserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(s => s.Branch).WithMany().HasForeignKey(s => s.BranchId).OnDelete(DeleteBehavior.Cascade);
        });

        // CompanyWidget
        modelBuilder.Entity<CompanyWidget>(entity =>
        {
            entity.HasKey(w => w.Id);
            entity.Property(w => w.WidgetId).IsRequired().HasMaxLength(64);
            entity.Property(w => w.WidgetTokenHash).HasMaxLength(64);
            entity.HasOne(w => w.Company).WithMany().HasForeignKey(w => w.CompanyId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(w => w.Branch).WithMany().HasForeignKey(w => w.BranchId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(w => w.WidgetId).IsUnique();
        });

        // CompanyClient
        modelBuilder.Entity<CompanyClient>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.FirstName).HasMaxLength(50);
            entity.Property(c => c.LastName).HasMaxLength(50);
            entity.Property(c => c.Email).HasMaxLength(255);
            entity.Property(c => c.Mobile).HasMaxLength(15);
            entity.Property(c => c.SessionTokenHash).IsRequired().HasMaxLength(64);
            entity.HasOne(c => c.Company).WithMany().HasForeignKey(c => c.CompanyId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(c => c.Branch).WithMany().HasForeignKey(c => c.BranchId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(c => c.CompanyWidget).WithMany().HasForeignKey(c => c.CompanyWidgetId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(c => c.SessionTokenHash);
            entity.HasIndex(c => c.CreatedAt);
        });

        // CompanyAiWidget
        modelBuilder.Entity<CompanyAiWidget>(entity =>
        {
            entity.HasKey(w => w.Id);
            entity.Property(w => w.TenantId).IsRequired().HasMaxLength(128);
            entity.HasOne(w => w.Company).WithMany().HasForeignKey(w => w.CompanyId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(w => w.Branch).WithMany().HasForeignKey(w => w.BranchId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(w => w.TenantId).IsUnique();
            entity.HasIndex(w => w.BranchId).IsUnique();
        });

        // AiContentBlock
        modelBuilder.Entity<AiContentBlock>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.ContentSnippet).IsRequired().HasMaxLength(500);
            entity.Property(b => b.Content).IsRequired();
            entity.Property(b => b.EmbeddingJson);
            entity.Property(b => b.Status).IsRequired().HasConversion<int>();
            entity.Property(b => b.ErrorMessage).HasMaxLength(2000);
            entity.HasOne(b => b.Branch).WithMany().HasForeignKey(b => b.BranchId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(b => b.CompanyAiWidget).WithMany().HasForeignKey(b => b.CompanyAiWidgetId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(b => b.JobId).HasFilter("\"JobId\" IS NOT NULL");
            entity.HasIndex(b => new { b.BranchId, b.CreatedAt });
        });

        // Story
        modelBuilder.Entity<Story>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(s => s.Items).WithOne(i => i.Story).HasForeignKey(i => i.StoryId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(s => s.Views).WithOne(v => v.Story).HasForeignKey(v => v.StoryId).OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(s => s.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(s => s.Views).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.HasIndex(s => new { s.UserId, s.ExpiresAt });
            entity.HasIndex(s => s.ExpiresAt);
        });

        modelBuilder.Entity<StoryItem>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.MediaType).IsRequired().HasMaxLength(20);
            entity.Property(i => i.ObjectKey).IsRequired().HasMaxLength(500);
            entity.Property(i => i.TextOverlayJson).HasMaxLength(2000);
            entity.HasIndex(i => new { i.StoryId, i.SortOrder });
        });

        modelBuilder.Entity<StoryView>(entity =>
        {
            entity.HasKey(v => v.Id);
            entity.HasOne(v => v.ViewerUser).WithMany().HasForeignKey(v => v.ViewerUserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(v => new { v.StoryId, v.ViewerUserId }).IsUnique();
        });
    }
}