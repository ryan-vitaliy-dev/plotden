using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using backend.Domain.Sessions;
using backend.Domain.Tokens;
using backend.Domain.Profiles;

namespace backend.Domain.Accounts
{
    public class Account
    {
        // [Key]
        // [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        // public long Id { get; set; }
        [Key]
        public Guid AccountId { get; set; } = Guid.CreateVersion7();
        
        [Required]
        public string Email { get; set; } = null!;

        [Required]
        public bool HasVerifiedEmail { get; set; } = false;

        [Required]
        public string PasswordHash { get; set; } = null!;

        [Required]
        public DateTime CreatedAt { get; set; }

        // Navigation properties

        public Profile Profile { get; set; } = null!;

        public ICollection<Session> Sessions { get; set; } = [];

        public ICollection<Token> TokensById { get; set; } = [];

        // public ICollection<UserToken> UserTokensByEmail { get; set; } = [];

    }
}