namespace Application.Auth.Results
{
    public record SigninResetPasswordResult(string SessionId, DateTimeOffset ExpiresAt);
}