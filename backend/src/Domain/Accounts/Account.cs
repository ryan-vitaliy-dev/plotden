using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Domain.Sessions;
using Domain.Tokens;
using Domain.Profiles;

namespace Domain.Accounts
{
    public class Account
    {
        // old stuff
        // [Key]
        // [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        // public long Id { get; set; }
        [Key]
        public Guid AccountId { get; set; } = Guid.CreateVersion7();
        
        [Required]
        public string Email { get; set; } = null!;

        public string? PasswordHash { get; set; }

        [Required]
        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? VerifiedAt { get; set; } = null;

        public DateTimeOffset? FinishedSignupAt { get; set; } = null;

        // Navigation properties

        public Profile Profile { get; set; } = null!;

        public ICollection<Session> Sessions { get; set; } = [];

        public ICollection<Token> TokensById { get; set; } = [];

        // public ICollection<UserToken> UserTokensByEmail { get; set; } = [];

    }
}