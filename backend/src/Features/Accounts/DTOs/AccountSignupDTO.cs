using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

using backend.Features.Accounts.ValidationAttributes;
using Microsoft.Extensions.Localization;

namespace backend.Features.Accounts.DTOs
{
    public class AccountSignupDTO
    {
        [JsonPropertyName("email")]
        [Required(ErrorMessageResourceName = "SignupMissingEmail", ErrorMessageResourceType = typeof(Resources.SharedResource))]
        [EmailAddress(ErrorMessageResourceName = "GeneralInvalidEmail", ErrorMessageResourceType = typeof(Resources.SharedResource))]
        [NotIANAReservedDomain(ErrorMessageResourceName = "GeneralInvalidEmail", ErrorMessageResourceType = typeof(Resources.SharedResource))]
        [MaxLength(320, ErrorMessageResourceName = "GeneralInvalidEmail", ErrorMessageResourceType = typeof(Resources.SharedResource))]
        public string Email { get; set; } = null!;

        [JsonPropertyName("password")]
        [Required(ErrorMessageResourceName = "SignupMissingPassword", ErrorMessageResourceType = typeof(Resources.SharedResource))]
        [RegularExpression(
            @"^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[!?@#$%^&\-_=+~:;]).{12,100}$", 
            ErrorMessageResourceName = "SignupInvalidPassword", ErrorMessageResourceType = typeof(Resources.SharedResource)
        )]
        public string Password { get; set; } = null!;
    }
}