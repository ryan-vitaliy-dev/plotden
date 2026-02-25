using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Localization;

namespace backend.Features.Accounts.ValidationAttributes {

    public class NotIANAReservedDomainAttribute : ValidationAttribute
    {
        private readonly HashSet<string> ReservedDomains = [
            "example.com",
            "example.net",
            "example.org"
        ];

        private readonly HashSet<string> ReservedTlds = [
            "test",
            "example",
            "invalid",
            "localhost"
        ];

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            // If the value is a string, assign it to variable "email"
            if (value is string email)
            {
                string[] parts = email.Split('@');
                if (parts.Length == 2)
                {
                    string domain = parts[1].ToLowerInvariant();

                    if (ReservedDomains.Contains(domain))
                    {
                        return new ValidationResult(""); // Returning empty string coz I dont know how to use localizer in here, I just added error message override in DTO
                    }

                    var tld = domain.Split('.').Last();
                    if (ReservedTlds.Contains(tld))
                    {
                        return new ValidationResult("");
                    }
                }
                else if(parts.Length < 2)
                {
                    return new ValidationResult("");
                }
            }
            return ValidationResult.Success;
        }
    }
}