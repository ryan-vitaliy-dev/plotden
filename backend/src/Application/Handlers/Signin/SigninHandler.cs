using System.Net;
using Application.Accounts;
using Application.Auth.Results;
using Application.Common;
using Application.Handlers.Common;
using Application.Sessions;
using Domain.Accounts;
using Domain.Common;
using Domain.Sessions;
using Domain.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Application.Handlers.Signup
{
    public class SigninHandler(AccountService accountService, SessionService sessionService, ILogger<SigninHandler> logger)
    {
        private readonly AccountService _accountService = accountService;
        private readonly SessionService _sessionService = sessionService;
        private readonly ILogger<SigninHandler> _logger = logger;
        
        private readonly PasswordHasher<Account> _passwordHasher = new();

        public async Task<ServiceResult<SigninResult>> HandleAsync(string email, string password, IPAddress? ipAddress, string? userAgent, CancellationToken clt)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return ServiceResult<SigninResult>.Failure(ServiceError.InvalidInput);
            }

            ServiceResult<Account> existingAccountCheckResult = await _accountService.FindAccountByEmailAsync(email, clt);
            Account account = existingAccountCheckResult.IsFailure 
                ? new Account()
                : existingAccountCheckResult.Value;

            PasswordVerificationResult passwordVerificationResult = _passwordHasher.VerifyHashedPassword(account, account.PasswordHash ?? string.Empty, password);
            if(existingAccountCheckResult.IsFailure || passwordVerificationResult == PasswordVerificationResult.Failed)
            {
                return ServiceResult<SigninResult>.Failure(ServiceError.InvalidCredentials);
            }
            // TODO: handle rehash stuff later if needed

            // TODO: Enforce a max session amount maybe per device later for production, just in case

            ServiceResult<Session> sessionCreationResult = await _sessionService.CreateSessionAsync(account.AccountId, SessionType.Standard, ipAddress, userAgent, null, clt);
            if(sessionCreationResult.IsFailure)
            {
                // If session creation fails, show error to user and tell them to try link again
                _logger.LogError("Session creation failed for account id {AccountId} after successful sign in. Error: {ErrorCode}", account.AccountId, sessionCreationResult.ErrorCode);
                return ServiceResult<SigninResult>.Failure(ServiceError.SessionCreationFailed);
            }
            Session createdSession = sessionCreationResult.Value;

            SigninResult result = new(createdSession.SessionId.ToString(), createdSession.ExpiresAt);

            return ServiceResult<SigninResult>.Success(result);
        }
    }
}