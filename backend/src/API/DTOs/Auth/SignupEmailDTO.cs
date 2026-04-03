using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using API.DTOs.ValidationAttributes;

namespace API.DTOs.Auth
{
    public class SignupEmailDTO
    {
        [JsonPropertyName("email")]
        [Required(ErrorMessageResourceName = "Signup_ErrorMissingEmail", ErrorMessageResourceType = typeof(Application.Resources.SharedResource))]
        [EmailAddress(ErrorMessageResourceName = "GeneralInvalidEmail", ErrorMessageResourceType = typeof(Application.Resources.SharedResource))]
        [NotIANAReservedDomain(ErrorMessageResourceName = "GeneralInvalidEmail", ErrorMessageResourceType = typeof(Application.Resources.SharedResource))]
        [MaxLength(320, ErrorMessageResourceName = "GeneralInvalidEmail", ErrorMessageResourceType = typeof(Application.Resources.SharedResource))]
        public string Email { get; set; } = null!;
    }
}