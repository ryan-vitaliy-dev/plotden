using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

using backend.Application.Accounts.ValidationAttributes;

namespace backend.API.DTOs.Auth
{
    public class SignupEmailDTO
    {
        [JsonPropertyName("email")]
        [Required(ErrorMessageResourceName = "Signup_ErrorMissingEmail", ErrorMessageResourceType = typeof(Resources.SharedResource))]
        [EmailAddress(ErrorMessageResourceName = "GeneralInvalidEmail", ErrorMessageResourceType = typeof(Resources.SharedResource))]
        [NotIANAReservedDomain(ErrorMessageResourceName = "GeneralInvalidEmail", ErrorMessageResourceType = typeof(Resources.SharedResource))]
        [MaxLength(320, ErrorMessageResourceName = "GeneralInvalidEmail", ErrorMessageResourceType = typeof(Resources.SharedResource))]
        public string Email { get; set; } = null!;
    }
}