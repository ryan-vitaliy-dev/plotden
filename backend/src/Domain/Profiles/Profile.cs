using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using backend.Domain.Sessions;
using backend.Domain.Accounts;

namespace backend.Domain.Profiles
{
    public class Profile
    {
        [Key]
        public Guid AccountId { get; set; }

        [Required]
        public string Username { get; set; } = null!;

        // Navigation properties

        public Account Account { get; set; } = null!;
    }
}