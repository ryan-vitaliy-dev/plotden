using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API.DTOs.Auth
{
    public class SignupPasswordDTO
    {
        [JsonPropertyName("password")]
        [Required(ErrorMessageResourceName = "Signup_ErrorMissingPassword", ErrorMessageResourceType = typeof(Application.Resources.SharedResource))]
        [RegularExpression(
            @"^(?=.*[A-Z])(?=.*[a-z])(?=.*[0-9])(?=.*[!?@#$%^&*\-_=+~:;])[A-Za-z0-9!?@#$%^&*\-_=+~:; ]{12,100}$", 
            ErrorMessageResourceName = "Signup_ErrorInvalidPassword", ErrorMessageResourceType = typeof(Application.Resources.SharedResource)
        )]
        public string Password { get; set; } = null!;

    }
}