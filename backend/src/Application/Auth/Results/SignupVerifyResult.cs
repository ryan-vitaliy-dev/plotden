namespace Application.Auth.Results
{
    public record SignupVerifyResult(string SessionId, DateTimeOffset ExpiresAt);
}