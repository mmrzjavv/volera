using Microsoft.EntityFrameworkCore;
using Core.Domain.Entities;
using Shared;

namespace Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Call> Calls { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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

        // Call
        modelBuilder.Entity<Call>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Status).IsRequired();
            entity.HasOne(c => c.Caller).WithMany().HasForeignKey(c => c.CallerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(c => c.Receiver).WithMany().HasForeignKey(c => c.ReceiverId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}