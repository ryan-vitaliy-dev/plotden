using System.Net;
using Application.Accounts;
using Application.Auth;
using Application.Auth.Results;
using Application.Common;
using Application.Sessions;
using Domain.Accounts;
using Domain.Common;
using Domain.Sessions;
using Microsoft.Extensions.Logging;

namespace Application.Handlers.Signin
{
    public class SigninApplyResetPasswordHandler(AccountService accountService, SessionService sessionService, AuthService authService, ILogger<SigninApplyResetPasswordHandler> logger)
    {
        private readonly AccountService _accountService = accountService;

        private readonly SessionService _sessionService = sessionService;
        private readonly AuthService _authService = authService;

        private readonly ILogger<SigninApplyResetPasswordHandler> _logger = logger;

        public async Task<ServiceResult<SigninResetPasswordResult>> HandleAsync(Guid accountId, string password, IPAddress? ipAddress, string? userAgent, CancellationToken clt)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return ServiceResult<SigninResetPasswordResult>.Failure(ServiceError.InvalidInput);
            }

            ServiceResult<Account> findAccountResult = await _accountService.FindAccountByIdAsync(accountId, clt);
            if(findAccountResult.IsFailure)
            {
                return ServiceResult<SigninResetPasswordResult>.Failure(ServiceError.InvalidInput);
            }
            Account account = findAccountResult.Value;

            ServiceResult<Unit> resetPasswordResult = await _authService.FinishResetPasswordAsync(account, password, clt);
            if(resetPasswordResult.IsFailure)
            {
                return ServiceResult<SigninResetPasswordResult>.Failure(resetPasswordResult.ErrorCode!.Value);
            }

            // TODO: Maybe check the ordering and see if this is okay, if transaction needed, etc
            // Invalidate all prior existing sessions
            ServiceResult<Unit> invalidateExistingSessionsResult = await _sessionService.InvalidateAllSessionsAsync(account.AccountId, clt);
            if(invalidateExistingSessionsResult.IsFailure)
            {
                _logger.LogError("Failed to invalidate existing sessions for account with id {AccountId}", account.AccountId);
                return ServiceResult<SigninResetPasswordResult>.Failure(invalidateExistingSessionsResult.ErrorCode!.Value);
            }

            // Create new standard session
            ServiceResult<Session> sessionCreationResult = await _sessionService.CreateSessionAsync(account.AccountId, SessionType.Standard, ipAddress, userAgent, null, clt);
            if(sessionCreationResult.IsFailure)
            {
                // If session creation fails, show error to user and tell them to try link again
                _logger.LogError("Session creation failed for account id {AccountId} after successfully applying password reset. Error: {ErrorCode}", account.AccountId, sessionCreationResult.ErrorCode);
                return ServiceResult<SigninResetPasswordResult>.Failure(ServiceError.SessionCreationFailed);
            }
            Session createdSession = sessionCreationResult.Value;

            SigninResetPasswordResult result = new(createdSession.SessionId.ToString(), createdSession.ExpiresAt);

            return ServiceResult<SigninResetPasswordResult>.Success(result);
        }
    }
}