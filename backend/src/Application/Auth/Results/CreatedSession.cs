namespace Application.Auth.Results
{
    public record CreatedSession(string SessionId, DateTimeOffset ExpiresAt);
}