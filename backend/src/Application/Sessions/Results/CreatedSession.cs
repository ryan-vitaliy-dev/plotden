namespace Application.Sessions.Results
{
    public record CreatedSession(string SessionId, DateTimeOffset ExpiresAt);
}