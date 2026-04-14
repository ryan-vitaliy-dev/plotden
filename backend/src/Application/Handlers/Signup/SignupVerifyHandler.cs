

using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Localization;

using Application.Accounts;
using Application.Auth.DTOs;
using Application.Common;
using Application.Sessions;
using Application.Tokens;
using Domain.Accounts;
using Domain.Sessions;
using Domain.Tokens;
using Domain.Common;
using Application.Resources;
using Microsoft.Extensions.Logging;
using Application.Auth;

namespace Application.Handlers.Signup
{
    public class SignupVerifyHandler(
        ILogger<SignupVerifyHandler> logger, 
        AuthService authService,
        AccountService accountService,
        TokenService tokenService,
        SessionService sessionService, 
        // IEmailSender emailSender,
        IStringLocalizer<SharedResource> localizer)
    {
        private readonly ILogger<SignupVerifyHandler> _logger = logger;

        private readonly TokenService _tokenService = tokenService;

        private readonly AccountService _accountService = accountService;
        private readonly AuthService _authService = authService;
        private readonly SessionService _sessionService = sessionService;
        private readonly IStringLocalizer<SharedResource> _localizer = localizer;

        public async Task<ServiceResult<SignupVerifyResult>> HandleAsync(string rawToken, IPAddress? ipAddress, string? userAgent, CancellationToken clt)
        {
            
            byte[] generatedTokenBytes = Encoding.UTF8.GetBytes(rawToken);
            byte[] tokenHashBytes = SHA256.HashData(generatedTokenBytes);
            string tokenHash = Convert.ToHexStringLower(tokenHashBytes);

            ServiceResult<Token> findMatchingTokenResult = await _tokenService.FindActiveTokenByHashAsync(tokenHash, clt);
            if(findMatchingTokenResult.IsFailure)
            {
                // Either a DB error such as a connection error or the token is invalid/expired
                return ServiceResult<SignupVerifyResult>.Failure(findMatchingTokenResult.ErrorCode!.Value);
            }
            Token matchingToken = findMatchingTokenResult.Value;

            ServiceResult<Account> findMatchingAccountResult = await _accountService.FindAccountByIdAsync(matchingToken.AccountId, clt);
            if(findMatchingAccountResult.IsFailure)
            {
                // Either a DB error such as connection error or some other unexpected error, log it for safety
                _logger.LogError("Could not find matching account with id {AccountId} for token hash {TokenHash}", matchingToken.AccountId, matchingToken.TokenHash);
                return ServiceResult<SignupVerifyResult>.Failure(findMatchingAccountResult.ErrorCode!.Value);
            }
            Account matchingAccount = findMatchingAccountResult.Value;

            ServiceResult<Unit> consumeTokenAndVerifyAccountResult = await _authService.ConsumeTokenAndVerifyAccountAsync(matchingToken, matchingAccount, clt);
            if(consumeTokenAndVerifyAccountResult.IsFailure)
            {
                // at this point, it's rolled back. show error.
                return ServiceResult<SignupVerifyResult>.Failure(consumeTokenAndVerifyAccountResult.ErrorCode!.Value);
            }

            // Create a session
            ServiceResult<Session> sessionCreationResult = await _sessionService.CreateSessionAsync(matchingAccount.AccountId, ipAddress, userAgent, null, clt);
            if(sessionCreationResult.IsFailure)
            {
                // If session creation fails, show error to user and tell them to try link again
                _logger.LogError("Session creation failed for account id {AccountId} after successful email verification. Error: {ErrorCode}", matchingAccount.AccountId, sessionCreationResult.ErrorCode);
                return ServiceResult<SignupVerifyResult>.Failure(ServiceError.SessionCreationFailed);
            }
            Session createdSession = sessionCreationResult.Value;

            SignupVerifyResult result = new(createdSession.SessionId.ToString(), createdSession.ExpiresAt);
            return ServiceResult<SignupVerifyResult>.Success(result);
        }
    }
}