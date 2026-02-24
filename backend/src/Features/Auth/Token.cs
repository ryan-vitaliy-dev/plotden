using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using backend.Features.Users;

namespace backend.Features.Auth
{
    public enum TokenType
    {
        EmailVerification,
        EmailUpdate,
        PasswordReset
    }

    public class Token
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        // Is a string instead of a Guid to allow for more secure random token IDs
        [Required]
        public string TokenId { get; set; } = null!;

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public string Email { get; set; } = null!;

        [Required]
        public TokenType TokenType { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime ExpiresAt { get; set; }

        public User UserById { get; set; } = null!;
    }
}