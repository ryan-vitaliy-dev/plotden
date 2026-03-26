using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using backend.Domain.Accounts;

namespace backend.Domain.Sessions
{
    public class Session
    {
        // [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        // public long Id { get; set; }

        [Key]
        [Required]
        public Guid SessionId { get; set; } = Guid.CreateVersion7();

        [Required]
        public Guid AccountId { get; set; }

        [Required]
        public DateTimeOffset CreatedAt { get; set; }

        [Required]
        public DateTimeOffset ExpiresAt { get; set; }

        [Required]
        public string UserAgent { get; set; } = null!;

        [Required]
        public string IpAddress { get; set; } = null!;

        [Required]
        public bool IsExpired { get; set; } = false;

        public Account Account { get; set; } = null!;
    }
}