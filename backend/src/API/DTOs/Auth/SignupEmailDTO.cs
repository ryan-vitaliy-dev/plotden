using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using API.DTOs.ValidationAttributes;

namespace API.DTOs.Auth
{
    public class SignupEmailDTO
    {
        [JsonPropertyName("email")]
        [Required(ErrorMessageResourceName = "Signup_Error_MissingEmail", ErrorMessageResourceType = typeof(Application.Resources.SharedResource))]
        [EmailAddress(ErrorMessageResourceName = "General_Error_InvalidEmail", ErrorMessageResourceType = typeof(Application.Resources.SharedResource))]
        [NotIANAReservedDomain(ErrorMessageResourceName = "General_Error_InvalidEmail", ErrorMessageResourceType = typeof(Application.Resources.SharedResource))]
        [MaxLength(320, ErrorMessageResourceName = "General_Error_InvalidEmail", ErrorMessageResourceType = typeof(Application.Resources.SharedResource))]
        public string Email { get; set; } = null!;
    }
}