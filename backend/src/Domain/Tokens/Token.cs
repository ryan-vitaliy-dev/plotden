using System.ComponentModel.DataAnnotations;

using Domain.Accounts;

namespace Domain.Tokens
{
    public enum TokenType
    {
        EmailVerification,
        ResumeSignup,
        EmailUpdate,
        PasswordReset
    }

    public class Token
    {
        [Key]
        [Required]
        public Guid TokenId { get; set; } = Guid.CreateVersion7();

        [Required]
        public string TokenHash { get; set; } = null!;

        [Required]
        public Guid AccountId { get; set; }

        [Required]
        public TokenType TokenType { get; set; }

        [Required]
        public DateTimeOffset CreatedAt { get; set; }

        [Required]
        public DateTimeOffset ExpiresAt { get; set; }

        public DateTimeOffset? ConsumedAt { get; set; }

        public DateTimeOffset? RevokedAt { get; set; }

        // Navigational Properties 
        public Account AccountById { get; set; } = null!;
    }
}