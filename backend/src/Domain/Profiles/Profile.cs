using System.ComponentModel.DataAnnotations;

using Domain.Accounts;

namespace Domain.Profiles
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