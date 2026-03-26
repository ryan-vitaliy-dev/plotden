namespace backend.Application.Accounts.DTOs
{
    public record AccountSignupResult(string Email, Guid SessionId, DateTimeOffset ExpiresAt);
}