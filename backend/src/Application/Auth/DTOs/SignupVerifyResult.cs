namespace backend.Application.Auth.DTOs
{
    public record SignupVerifyResult(string SessionId, DateTimeOffset ExpiresAt);
}