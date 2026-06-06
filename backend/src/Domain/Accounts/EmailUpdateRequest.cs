
using System.ComponentModel.DataAnnotations;

namespace Domain.Accounts
{
    public class EmailUpdateRequest
    {
        [Key]
        public Guid EmailUpdateRequestId { get; set; } = Guid.CreateVersion7();
        
        [Required]
        public Guid AccountId { get; set; }

        [Required]
        public string OldEmail { get; set; } = string.Empty;

        [Required]
        public string NewEmail { get; set; } = string.Empty;

        [Required]
        public DateTimeOffset RequestedAt { get; set; }

        // public DateTimeOffset? CompletedAt { get; set; } = null; // For now, we just delete an entry when it is completed. Later, we can archive if needed

        // Navigation properties
        public Account Account { get; set; } = null!;
    }
}