using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using backend.Features.Sessions;
using backend.Features.Auth;

namespace backend.Features.Users
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        public Guid UserId { get; set; } = Guid.CreateVersion7();

        [Required]
        public string Email { get; set; } = null!;

        [Required]
        public string PasswordHash { get; set; } = null!;

        [Required]
        public string Username { get; set; } = null!;

        [Required]
        public bool IsVerified { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; }

        // Navigation properties

        public ICollection<Session> Sessions { get; set; } = [];

        public ICollection<Token> TokensById { get; set; } = [];

        // public ICollection<UserToken> UserTokensByEmail { get; set; } = [];

    }
}