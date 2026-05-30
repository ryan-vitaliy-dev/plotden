using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Application.Common.Interfaces;
using Application.Resources;
using Domain.Sessions;

namespace Infrastructure.Auth
{
    public class SessionAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IStringLocalizer<SharedResource> localizer,
        IAppDbContext appDbContext
    ) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        private string? failureMessage;
        private readonly IAppDbContext _appDbContext = appDbContext;
        private readonly IStringLocalizer<SharedResource> _localizer = localizer;

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Cookies.TryGetValue("sid", out string? sessionId) || string.IsNullOrEmpty(sessionId))
            {
                failureMessage = _localizer["General_Error_MissingSession"];
                return AuthenticateResult.Fail(failureMessage);
            }

            if(!Guid.TryParse(sessionId, out Guid parsedGuid))
            {
                failureMessage = _localizer["General_Error_MissingSession"];
                return AuthenticateResult.Fail(failureMessage);
            }

            Session? session = await _appDbContext.Sessions.FirstOrDefaultAsync(s => s.SessionId == parsedGuid);
            if(session == null)
            {
                failureMessage = _localizer["General_Error_MissingSession"];
                return AuthenticateResult.Fail(failureMessage);
            }

            bool isRevoked = session.RevokedAt != null;
            bool isExpired = session.ExpiresAt <= DateTimeOffset.UtcNow;
            bool isValidSession = !isRevoked && !isExpired;
            if(!isValidSession)
            {
                failureMessage = _localizer["General_Error_MissingSession"];
                return AuthenticateResult.Fail(failureMessage);
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, session.AccountId.ToString()),
                new Claim("SessionId", session.SessionId.ToString()),
                new Claim("SessionType", session.SessionType.ToString())
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Endpoint? endpoint = Context.GetEndpoint();
            if (endpoint != null && endpoint.DisplayName != null && endpoint.DisplayName?.Contains("GetUserAsync") == true)
            {
                Response.StatusCode = StatusCodes.Status204NoContent;
                return;
            }
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            Response.ContentType = "application/json";

            if(failureMessage != null && failureMessage is string str)
            {
                var newResponseBody = new
                {
                    message = str
                };
                await System.Text.Json.JsonSerializer.SerializeAsync(Response.Body, newResponseBody);
            }
            return;
        }
    }
}