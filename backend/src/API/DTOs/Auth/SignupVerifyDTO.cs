using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API.DTOs.Auth
{
    public class SignupVerifyDTO
    {
        [JsonPropertyName("token")]
        [Required(ErrorMessageResourceName = "Signup_ErrorMissingToken", ErrorMessageResourceType = typeof(Application.Resources.SharedResource))]
        [StringLength(32, MinimumLength = 32, ErrorMessageResourceName = "Signup_ErrorInvalidToken", ErrorMessageResourceType = typeof(Application.Resources.SharedResource))]
        public string Token { get; set; } = null!;
    }
}