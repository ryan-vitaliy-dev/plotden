namespace backend.Application.Accounts.DTOs
{
    public record AccountSignupVerifyResult(string SessionId, DateTimeOffset ExpiresAt);
}