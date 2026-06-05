using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API.DTOs.Auth
{
    public class SigninDTO
    {
        [JsonPropertyName("email")]
        [Required(ErrorMessageResourceName = "General_Error_MissingEmail", ErrorMessageResourceType = typeof(Application.Resources.SharedResource))]
        [EmailAddress(ErrorMessageResourceName = "General_Error_InvalidEmail", ErrorMessageResourceType = typeof(Application.Resources.SharedResource))]
        [MaxLength(320, ErrorMessageResourceName = "General_Error_InvalidEmail", ErrorMessageResourceType = typeof(Application.Resources.SharedResource))]
        public string Email { get; set; } = null!;

        [JsonPropertyName("password")]
        [Required(ErrorMessageResourceName = "Signin_Error_MissingPassword", ErrorMessageResourceType = typeof(Application.Resources.SharedResource))]
        [MaxLength(100, ErrorMessageResourceName = "General_Error_InvalidPassword", ErrorMessageResourceType = typeof(Application.Resources.SharedResource))]
        public string Password { get; set; } = null!;
    }
}