using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace backend.API.DTOs.Auth
{
    public class SignupVerifyDTO
    {
        [JsonPropertyName("token")]
        [Required(ErrorMessageResourceName = "Signup_ErrorMissingToken", ErrorMessageResourceType = typeof(Resources.SharedResource))]
        [StringLength(32, MinimumLength = 32, ErrorMessageResourceName = "Signup_ErrorInvalidToken", ErrorMessageResourceType = typeof(Resources.SharedResource))]
        public string Token { get; set; } = null!;
    }
}