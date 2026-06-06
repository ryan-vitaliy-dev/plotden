using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API.DTOs.Account
{
    public class UpdateEmailDTO
    {
        [JsonPropertyName("new_email")]
        [Required(ErrorMessageResourceName = "General_Error_MissingEmail", ErrorMessageResourceType = typeof(Application.Resources.SharedResource))]
        [EmailAddress(ErrorMessageResourceName = "General_Error_InvalidEmail", ErrorMessageResourceType = typeof(Application.Resources.SharedResource))]
        [MaxLength(320, ErrorMessageResourceName = "General_Error_InvalidEmail", ErrorMessageResourceType = typeof(Application.Resources.SharedResource))]
        public string NewEmail { get; set; } = null!;

        [JsonPropertyName("password")]
        // TODO: Fix this key to use a more general missing password resource
        [Required(ErrorMessageResourceName = "Account_UpdatePassword_Error_MissingCurrent", ErrorMessageResourceType = typeof(Application.Resources.SharedResource))]
        [MaxLength(100, ErrorMessageResourceName = "General_Error_InvalidPassword", ErrorMessageResourceType = typeof(Application.Resources.SharedResource))]
        public string Password { get; set; } = null!;
    }
}