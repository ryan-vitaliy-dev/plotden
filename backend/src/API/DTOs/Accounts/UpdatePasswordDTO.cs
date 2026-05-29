using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API.DTOs.Accounts
{
    public class UpdatePasswordDTO
    {
        [JsonPropertyName("current")]
        [Required(ErrorMessageResourceName = "Settings_UpdatePassword_Error_MissingCurrent", ErrorMessageResourceType = typeof(Application.Resources.SharedResource))]
        [MaxLength(100, ErrorMessageResourceName = "Settings_UpdatePassword_Error_InvalidCurrent", ErrorMessageResourceType = typeof(Application.Resources.SharedResource))]
        public string CurrentPassword { get; set; } = null!;

        [JsonPropertyName("new")]
        [Required(ErrorMessageResourceName = "Settings_UpdatePassword_Error_MissingNew", ErrorMessageResourceType = typeof(Application.Resources.SharedResource))]
        [RegularExpression(
            @"^(?=.*[A-Z])(?=.*[a-z])(?=.*[0-9])(?=.*[!?@#$%^&*\-_=+~:;])[A-Za-z0-9!?@#$%^&*\-_=+~:; ]{12,100}$", 
            ErrorMessageResourceName = "General_Error_InvalidPassword", ErrorMessageResourceType = typeof(Application.Resources.SharedResource)
        )]
        public string NewPassword { get; set; } = null!;
    }
}