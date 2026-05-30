using System.ComponentModel.DataAnnotations;
using System.Net;

using Domain.Accounts;

namespace Domain.Sessions
{
    public enum SessionType
    {
        Standard,
        IncompleteSignup, // can only access signup resume route
        PasswordReset // can only access reset password route
    }

    public class Session
    {
        [Key]
        [Required]
        public Guid SessionId { get; set; } = Guid.CreateVersion7();

        [Required]
        public Guid AccountId { get; set; }

        [Required]
        public SessionType SessionType { get; set; }

        [Required]
        public DateTimeOffset CreatedAt { get; set; }

        [Required]
        public DateTimeOffset ExpiresAt { get; set; }

        public DateTimeOffset? RevokedAt { get; set; } = null;

        public string? UserAgent { get; set; } = null!;

        public IPAddress? IpAddress { get; set; } = null!;

        // Navigation properties

        public Account Account { get; set; } = null!;
    }
}