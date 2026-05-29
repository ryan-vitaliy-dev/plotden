namespace Application.Auth.Results
{
    public record SigninResult(string SessionId, DateTimeOffset ExpiresAt);
}