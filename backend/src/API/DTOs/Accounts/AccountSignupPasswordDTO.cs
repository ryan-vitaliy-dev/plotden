using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace backend.API.DTOs.Accounts
{
    public class AccountSignupPasswordDTO
    {
        [JsonPropertyName("password")]
        [Required(ErrorMessageResourceName = "Signup_MissingPassword", ErrorMessageResourceType = typeof(Resources.SharedResource))]
        [RegularExpression(
            @"^(?=.*[A-Z])(?=.*[a-z])(?=.*[0-9])(?=.*[!?@#$%^&*\-_=+~:;])[A-Za-z0-9!?@#$%^&*\-_=+~:; ]{12,100}$", 
            ErrorMessageResourceName = "Signup_InvalidPassword", ErrorMessageResourceType = typeof(Resources.SharedResource)
        )]
        public string Password { get; set; } = null!;

    }
}