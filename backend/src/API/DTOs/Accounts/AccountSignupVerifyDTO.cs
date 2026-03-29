using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace backend.API.DTOs.Accounts
{
    public class AccountSignupVerifyDTO
    {
        [JsonPropertyName("token")]
        [Required(ErrorMessageResourceName = "Signup_MissingToken", ErrorMessageResourceType = typeof(Resources.SharedResource))]
        [StringLength(32, MinimumLength = 32, ErrorMessageResourceName = "Signup_InvalidToken", ErrorMessageResourceType = typeof(Resources.SharedResource))]
        public string Token { get; set; } = null!;
    }
}