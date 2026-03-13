using GitPulse.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GitPulse.Api.Data;

public class GitPulseDbContext : DbContext
{
    public GitPulseDbContext(DbContextOptions<GitPulseDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Repo> Repos => Set<Repo>();
    public DbSet<Commit> Commits => Set<Commit>();
    public DbSet<DailySnapshot> DailySnapshots => Set<DailySnapshot>();
    public DbSet<SyncLog> SyncLogs => Set<SyncLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.GitHubId).IsUnique();
            e.HasIndex(u => u.Login).IsUnique();
        });

        modelBuilder.Entity<Repo>(e =>
        {
            e.HasIndex(r => r.GitHubId).IsUnique();
            e.HasOne(r => r.User).WithMany(u => u.Repos).HasForeignKey(r => r.UserId);
        });

        modelBuilder.Entity<Commit>(e =>
        {
            e.HasIndex(c => c.Sha).IsUnique();
            e.HasOne(c => c.Repo).WithMany(r => r.Commits).HasForeignKey(c => c.RepoId);
        });

        modelBuilder.Entity<DailySnapshot>(e =>
        {
            e.HasIndex(s => new { s.UserId, s.Date }).IsUnique();
            e.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId);
        });

        modelBuilder.Entity<SyncLog>(e =>
        {
            e.HasIndex(l => l.StartedAt);
            e.HasOne(l => l.User).WithMany().HasForeignKey(l => l.UserId);
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasIndex(r => r.Token).IsUnique();
            e.HasIndex(r => r.UserId);
            e.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId);
        });
    }
}
