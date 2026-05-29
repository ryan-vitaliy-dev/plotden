using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API.DTOs.Auth
{
    public class SignupResumeDTO
    {
        [JsonPropertyName("token")]
        [Required(ErrorMessageResourceName = "Signup_Error_MissingToken", ErrorMessageResourceType = typeof(Application.Resources.SharedResource))]
        [StringLength(32, MinimumLength = 32, ErrorMessageResourceName = "Signup_Error_InvalidToken", ErrorMessageResourceType = typeof(Application.Resources.SharedResource))]
        public string Token { get; set; } = null!;
    }
}