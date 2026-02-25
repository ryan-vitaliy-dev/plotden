using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using backend.Features.Accounts;

namespace backend.Features.Sessions
{
    public class Session
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        // Is a string instead of a Guid to allow for more secure random session IDs
        [Required]
        public string SessionId { get; set; } = null!;

        [Required]
        public Guid AccountId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime ExpiresAt { get; set; }

        [Required]
        public string UserAgent { get; set; } = null!;

        [Required]
        public string IpAddress { get; set; } = null!;

        [Required]
        public bool IsExpired { get; set; } = false;

        public Account Account { get; set; } = null!;
    }
}