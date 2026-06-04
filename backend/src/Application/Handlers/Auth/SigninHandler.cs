using System.Net;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using Application.Accounts;
using Application.Auth.Results;
using Application.Sessions;
using Domain.Accounts;
using Domain.Common;
using Domain.Sessions;
using Application.Common;

namespace Application.Handlers.Auth
{
    public class SigninHandler(
        ILogger<SigninHandler> logger,
        AccountService accountService, 
        SessionService sessionService
    )
    {
        private readonly ILogger<SigninHandler> _logger = logger;

        private readonly AccountService _accountService = accountService;
        private readonly SessionService _sessionService = sessionService;

        private readonly PasswordHasher<Account> _passwordHasher = new();

        public async Task<ServiceResult<CreatedSession>> HandleAsync(string email, string password, ClientInfo clientInfo, CancellationToken clt)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return ServiceResult<CreatedSession>.Failure(ServiceError.InvalidInput);
            }

            ServiceResult<Account> existingAccountCheckResult = await _accountService.FindAccountByEmailAsync(email, clt);
            Account account = existingAccountCheckResult.IsFailure 
                ? new Account()
                : existingAccountCheckResult.Value;

            PasswordVerificationResult passwordVerificationResult = _passwordHasher.VerifyHashedPassword(account, account.PasswordHash ?? string.Empty, password);
            if(existingAccountCheckResult.IsFailure || passwordVerificationResult == PasswordVerificationResult.Failed)
            {
                return ServiceResult<CreatedSession>.Failure(ServiceError.InvalidCredentials);
            }
            // TODO: handle rehash stuff later if needed

            // TODO: Enforce a max session amount maybe per device later for production, just in case

            ServiceResult<Session> sessionCreationResult = await _sessionService.CreateSessionAsync(
                account.AccountId, 
                SessionType.Standard, 
                clientInfo, 
                null, 
                clt
            );
            if(sessionCreationResult.IsFailure)
            {
                // If session creation fails, show error to user and tell them to try link again
                _logger.LogError(
                    "Session creation failed for account id {AccountId} after successful sign in. Error: {ErrorCode}", 
                    account.AccountId, 
                    sessionCreationResult.ErrorCode
                );
                return ServiceResult<CreatedSession>.Failure(ServiceError.SessionCreationFailed);
            }
            Session createdSession = sessionCreationResult.Value;

            CreatedSession result = new(createdSession.SessionId.ToString(), createdSession.ExpiresAt);

            return ServiceResult<CreatedSession>.Success(result);
        }
    }
}