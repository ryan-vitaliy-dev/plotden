using Microsoft.EntityFrameworkCore;

using backend.Features.Accounts;
using backend.Features.Auth;
using backend.Features.Sessions;

namespace backend.Infrastructure.Persistence
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options) {
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Token> Tokens { get; set; }
        public DbSet<Session> Sessions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<Blog>().OwnsMany(b => b.Posts, b => b.ToJson());

            modelBuilder.Entity<Account>(entity => {
                entity.ToTable("accounts", "core");
                entity.HasIndex(u => u.AccountId).IsUnique();
                // entity.HasIndex(u => u.Email).IsUnique(); Trying non-unique emails approach to avoid email enumeration and provide less friction. Guid is the source of truth.
                // entity.HasIndex(u => u.Username).IsUnique(); Updated usernames to no longer be required to be unique to allow users to have the same display name. Guid is the source of truth.
            });

            modelBuilder.Entity<Token>(entity =>
            {
                entity.ToTable("tokens", "core");
                entity.HasIndex(ut => ut.TokenId).IsUnique();
                entity
                    .HasOne(ut => ut.AccountById).WithMany(u => u.TokensById)
                    .HasForeignKey(ut => ut.AccountId).HasPrincipalKey(u => u.AccountId)
                    .OnDelete(DeleteBehavior.Cascade);
                //entity
                    // .HasOne(ut => ut.UserByEmail).WithMany(u => u.UserTokensByEmail)
                    // .HasForeignKey(ut => ut.Email).HasPrincipalKey(u => u.Email)
                    // .OnDelete(DeleteBehavior.Cascade);
                    // Updated to only have foreign key relationship with UserId to avoid complications with non-unique emails. Guid is the source of truth.
            });

            modelBuilder.Entity<Session>(entity =>
            {
                entity.ToTable("sessions", "core");
                entity.HasIndex(u => u.SessionId).IsUnique();
                entity
                    .HasOne(s => s.Account).WithMany(u => u.Sessions)
                    .HasForeignKey(s => s.AccountId).HasPrincipalKey(u => u.AccountId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}