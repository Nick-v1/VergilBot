using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VergilBot.Models;

namespace VergilBot.Services.Context;

public class VergilDbContext : DbContext
{
    private readonly IConfiguration _config;

    public VergilDbContext(IConfiguration configuration)
    {
        _config = configuration;
    }
    
    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured) optionsBuilder.UseNpgsql(_config.GetConnectionString("DefaultConnection"));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("user_accounts");
            entity.Property(u => u.Id).HasColumnName("id");
            entity.Property(u => u.Username).HasColumnName("username");
            entity.Property(u => u.Balance).HasColumnName("balance");
            entity.Property(u => u.DiscordId).HasColumnName("discord_account_id");
            entity.Property(u => u.HasSubscription).HasColumnName("hassubscription");
        });

    }
}