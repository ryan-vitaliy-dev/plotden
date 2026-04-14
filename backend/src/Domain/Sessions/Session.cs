using System.ComponentModel.DataAnnotations;
using System.Net;

using Domain.Accounts;

namespace Domain.Sessions
{
    public class Session
    {
        [Key]
        [Required]
        public Guid SessionId { get; set; } = Guid.CreateVersion7();

        [Required]
        public Guid AccountId { get; set; }

        [Required]
        public DateTimeOffset CreatedAt { get; set; }

        [Required]
        public DateTimeOffset ExpiresAt { get; set; }

        public string? UserAgent { get; set; } = null!;

        public IPAddress? IpAddress { get; set; } = null!;

        public DateTimeOffset? RevokedAt { get; set; } = null;

        // Navigation properties

        public Account Account { get; set; } = null!;
    }
}