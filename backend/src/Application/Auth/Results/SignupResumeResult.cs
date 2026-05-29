namespace Application.Auth.Results
{
    public record SignupResumeResult(string SessionId, DateTimeOffset ExpiresAt);
}