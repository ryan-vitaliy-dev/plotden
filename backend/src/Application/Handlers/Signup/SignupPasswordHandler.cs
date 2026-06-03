using System.Net;

using Microsoft.Extensions.Logging;

using Application.Accounts;
using Application.Auth;
using Application.Auth.Results;
using Application.Common;
using Application.Sessions;
using Domain.Accounts;
using Domain.Common;
using Domain.Sessions;

namespace Application.Handlers.Signup
{
    public class SignupPasswordHandler(AccountService accountService, SessionService sessionService, AuthService authService, ILogger<SignupPasswordHandler> logger)
    {
        private readonly AccountService _accountService = accountService;
        private readonly SessionService _sessionService = sessionService;
        private readonly AuthService _authService = authService;

        private readonly ILogger<SignupPasswordHandler> _logger = logger;

        // NOTE: Returns CreatedSession for now, we need a unified session return result for later to prevent duplicated code
        public async Task<ServiceResult<CreatedSession>> HandleAsync(Guid accountId, string password, IPAddress? ipAddress, string? userAgent, CancellationToken clt)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return ServiceResult<CreatedSession>.Failure(ServiceError.InvalidInput);
            }

            ServiceResult<Account> findAccountResult = await _accountService.FindAccountByIdAsync(accountId, clt);
            if(findAccountResult.IsFailure)
            {
                return ServiceResult<CreatedSession>.Failure(ServiceError.InvalidInput);
            }
            Account account = findAccountResult.Value;
            
            bool isNotVerified = account.VerifiedAt == null;
            bool alreadySetPassword = account.PasswordHash != null || account.FinishedSignupAt != null;

            if(isNotVerified)
            {
                return ServiceResult<CreatedSession>.Failure(ServiceError.AccountNotVerified);
            }

            if(alreadySetPassword)
            {
                return ServiceResult<CreatedSession>.Failure(ServiceError.PasswordAlreadySet);
            }

            ServiceResult<Unit> finishAccountSignupResult = await _authService.FinishAccountSignupAsync(account, password, clt);
            if(finishAccountSignupResult.IsFailure)
            {
                return ServiceResult<CreatedSession>.Failure(finishAccountSignupResult.ErrorCode!.Value);
            }

            ServiceResult<Unit> invalidateExistingSessionsResult = await _sessionService.InvalidateAllSessionsAsync(account.AccountId, clt);
            if(invalidateExistingSessionsResult.IsFailure)
            {
                _logger.LogError("Failed to invalidate existing sessions for account with id {AccountId}", account.AccountId);
                return ServiceResult<CreatedSession>.Failure(invalidateExistingSessionsResult.ErrorCode!.Value);
            }

            // Create new standard session
            ServiceResult<Session> sessionCreationResult = await _sessionService.CreateSessionAsync(account.AccountId, SessionType.Standard, ipAddress, userAgent, null, clt);
            if(sessionCreationResult.IsFailure)
            {
                // If session creation fails, show error to user and tell them to try link again
                _logger.LogError("Session creation failed for account id {AccountId} after successfully applying password reset. Error: {ErrorCode}", account.AccountId, sessionCreationResult.ErrorCode);
                return ServiceResult<CreatedSession>.Failure(ServiceError.SessionCreationFailed);
            }
            Session createdSession = sessionCreationResult.Value;

            CreatedSession result = new(createdSession.SessionId.ToString(), createdSession.ExpiresAt);

            return ServiceResult<CreatedSession>.Success(result);
        }
    }
}