using Microsoft.EntityFrameworkCore;

using Application.Common.Interfaces;
using Domain.Accounts;
using Domain.Tokens;
using Domain.Sessions;
using Domain.Profiles;

namespace Infrastructure.Persistence
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext {
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Profile> Profiles { get; set; }
        public DbSet<Token> Tokens { get; set; }
        public DbSet<Session> Sessions { get; set; }

        public DbSet<EmailUpdateRequest> EmailUpdateRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>(entity => 
            {
                entity.ToTable("accounts", "core");
                entity.HasKey(a => a.AccountId);
                entity.HasIndex(a => a.Email).IsUnique();
                entity
                    .HasOne(a => a.Profile).WithOne(p => p.Account)
                    .HasForeignKey<Profile>(p => p.AccountId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity
                    .HasMany(a => a.EmailUpdateRequests).WithOne(eur => eur.Account)
                    .HasForeignKey(eur => eur.AccountId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Profile>(entity => {
                entity.ToTable("profiles", "core");
                entity.HasKey(p => p.AccountId);
                //entity.HasIndex(p => p.AccountId).IsUnique(); removed since dont think I need unique check again if accountid is already unique in accounts table
                entity.HasIndex(p => p.AccountId);
            });

            modelBuilder.Entity<Session>(entity =>
            {
                entity.ToTable("sessions", "core");
                entity.HasKey(s => s.SessionId);
                entity
                    .HasOne(s => s.Account).WithMany(u => u.Sessions)
                    .HasForeignKey(s => s.AccountId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Token>(entity =>
            {
                entity.ToTable("tokens", "core");
                entity.HasKey(t => t.TokenId);
                entity.HasIndex(t => t.TokenHash).IsUnique();
                entity
                    .HasOne(t => t.AccountById).WithMany(a => a.TokensById)
                    .HasForeignKey(t => t.AccountId).HasPrincipalKey(a => a.AccountId)
                    .OnDelete(DeleteBehavior.Cascade);
                //entity
                    // .HasOne(ut => ut.UserByEmail).WithMany(u => u.UserTokensByEmail)
                    // .HasForeignKey(ut => ut.Email).HasPrincipalKey(u => u.Email)
                    // .OnDelete(DeleteBehavior.Cascade);
                    // Updated to only have foreign key relationship with UserId to avoid complications with non-unique emails. Guid is the source of truth.
            });

            modelBuilder.Entity<EmailUpdateRequest>(entity => 
            {
                entity.ToTable("email_update_requests", "core");
                entity.HasKey(eur => eur.EmailUpdateRequestId);
                entity.HasIndex(eur => eur.AccountId);
            });
        }
    }
}