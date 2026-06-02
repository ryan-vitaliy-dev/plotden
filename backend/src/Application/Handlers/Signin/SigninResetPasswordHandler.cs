using System.Net;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

using Application.Accounts;
using Application.Resources;
using Application.Auth.Results;
using Application.Common;
using Application.Sessions;
using Application.Tokens;
using Application.Auth;
using Domain.Accounts;
using Domain.Sessions;
using Domain.Tokens;
using Domain.Common;

namespace Application.Handlers.Signup
{
    public class SigninResetPasswordHandler(
        ILogger<SigninResetPasswordHandler> logger, 
        AuthService authService,
        AccountService accountService,
        TokenService tokenService,
        SessionService sessionService, 
        // IEmailSender emailSender,
        IStringLocalizer<SharedResource> localizer)
    {
        private readonly ILogger<SigninResetPasswordHandler> _logger = logger;

        private readonly TokenService _tokenService = tokenService;
        private readonly AccountService _accountService = accountService;
        private readonly AuthService _authService = authService;
        private readonly SessionService _sessionService = sessionService;
        private readonly IStringLocalizer<SharedResource> _localizer = localizer;

        public async Task<ServiceResult<SigninResetPasswordResult>> HandleAsync(string rawToken, IPAddress? ipAddress, string? userAgent, CancellationToken clt)
        {
            
            byte[] generatedTokenBytes = Encoding.UTF8.GetBytes(rawToken);
            byte[] tokenHashBytes = SHA256.HashData(generatedTokenBytes);
            string tokenHash = Convert.ToHexStringLower(tokenHashBytes);

            ServiceResult<Token> findMatchingTokenResult = await _tokenService.FindActiveTokenByHashAsync(tokenHash, clt);
            if(findMatchingTokenResult.IsFailure)
            {
                // Either a DB error such as a connection error or the token is invalid/expired
                return ServiceResult<SigninResetPasswordResult>.Failure(findMatchingTokenResult.ErrorCode!.Value);
            }
            Token matchingToken = findMatchingTokenResult.Value;

            ServiceResult<Account> findMatchingAccountResult = await _accountService.FindAccountByIdAsync(matchingToken.AccountId, clt);
            if(findMatchingAccountResult.IsFailure)
            {
                // Either a DB error such as connection error or some other unexpected error, log it for safety
                _logger.LogError("Could not find matching account with id {AccountId} for token hash {TokenHash}", matchingToken.AccountId, matchingToken.TokenHash);
                return ServiceResult<SigninResetPasswordResult>.Failure(findMatchingAccountResult.ErrorCode!.Value);
            }
            Account matchingAccount = findMatchingAccountResult.Value;

            // Invalidate all prior existing sessions
            ServiceResult<Unit> invalidateExistingSessionsResult = await _sessionService.InvalidateAllSessionsAsync(matchingAccount.AccountId, clt);
            if(invalidateExistingSessionsResult.IsFailure)
            {
                _logger.LogError("Failed to invalidate existing sessions for account with id {AccountId}", matchingToken.AccountId);
                return ServiceResult<SigninResetPasswordResult>.Failure(invalidateExistingSessionsResult.ErrorCode!.Value);
            }

            // TODO: May need to make this safer in terms of rolling back or something? Its not at the db level so cant use transaction but maybe there
            // is something else I can do
            ServiceResult<Unit> consumeTokenAndResumeSignupResult = await _authService.ConsumeTokenAndResumeAccountSignupAsync(matchingToken, matchingAccount, clt);
            if(consumeTokenAndResumeSignupResult.IsFailure)
            {
                // Couldn't consume the token for some reason
                return ServiceResult<SigninResetPasswordResult>.Failure(consumeTokenAndResumeSignupResult.ErrorCode!.Value);
            }

            // TODO: See if I should move this above the other check, to prevent failed of session creation after consuming token?
            // Create a session
            ServiceResult<Session> sessionCreationResult = await _sessionService.CreateSessionAsync(matchingAccount.AccountId, SessionType.PasswordReset, ipAddress, userAgent, null, clt);
            if(sessionCreationResult.IsFailure)
            {
                // If session creation fails, show error to user and tell them to try link again
                _logger.LogError("Session creation failed for account id {AccountId} after successful password reset email verification. Error: {ErrorCode}", matchingAccount.AccountId, sessionCreationResult.ErrorCode);
                return ServiceResult<SigninResetPasswordResult>.Failure(ServiceError.SessionCreationFailed);
            }
            Session createdSession = sessionCreationResult.Value;

            SigninResetPasswordResult result = new(createdSession.SessionId.ToString(), createdSession.ExpiresAt);
            return ServiceResult<SigninResetPasswordResult>.Success(result);
        }
    }
}