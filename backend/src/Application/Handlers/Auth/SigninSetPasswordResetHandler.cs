using Microsoft.Extensions.Logging;

using Application.Accounts;
using Application.Common;
using Application.Sessions;
using Domain.Accounts;
using Domain.Common;
using Domain.Sessions;
using Application.Common.Interfaces;
using Application.Sessions.Results;

namespace Application.Handlers.Auth
{
    public class SigninSetPasswordResetHandler(
        ILogger<SigninSetPasswordResetHandler> logger,
        IUnitOfWork unitOfWork,
        AccountService accountService, 
        SessionService sessionService
    )
    {
        private readonly ILogger<SigninSetPasswordResetHandler> _logger = logger;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        private readonly AccountService _accountService = accountService;
        private readonly SessionService _sessionService = sessionService;

        public async Task<ServiceResult<CreatedSession>> HandleAsync(Guid accountId, string password, ClientInfo clientInfo, CancellationToken clt)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return ServiceResult<CreatedSession>.Failure(ServiceError.InvalidInput);
            }

            ServiceResult<Account> findAccountResult = await _accountService.FindAccountByIdAsync(accountId, clt);
            if(findAccountResult.IsFailure)
            {
                // TODO: Do we need a logger here?
                return ServiceResult<CreatedSession>.Failure(ServiceError.InvalidInput);
            }
            Account account = findAccountResult.Value;


            await using var tx = await _unitOfWork.BeginTransactionAsync(clt);

            ServiceResult<Unit> setPasswordResult = await _accountService.SetPasswordAsync(account, password, clt);
            if(setPasswordResult.IsFailure)
            {
                await tx.RollbackAsync(CancellationToken.None);
                _logger.LogError(
                    "Failed to set new password for account with id {AccountId} - Error: {ErrorCode}",
                    account.AccountId,
                    setPasswordResult.ErrorCode
                );
                return ServiceResult<CreatedSession>.Failure(setPasswordResult.ErrorCode!.Value); // TODO: Check if this is ok
            }


            ServiceResult<Unit> invalidateSessionsResult = await _sessionService.InvalidateAllSessionsAsync(account.AccountId, clt);
            if(invalidateSessionsResult.IsFailure)
            {
                await tx.RollbackAsync(CancellationToken.None);
                _logger.LogError(
                    "Failed to invalidate sessions for account with id {AccountId} - Error: {ErrorCode}",
                    account.AccountId,
                    invalidateSessionsResult.ErrorCode
                );
                return ServiceResult<CreatedSession>.Failure(invalidateSessionsResult.ErrorCode!.Value); // TODO: Check if this is ok
            }


            ServiceResult<Session> createSessionResult = await _sessionService.CreateSessionAsync(
                account.AccountId, 
                SessionType.Standard, 
                clientInfo, 
                null, 
                clt
            );
            if(createSessionResult.IsFailure)
            {
                await tx.RollbackAsync(CancellationToken.None);
                _logger.LogError(
                    "Failed to create session for account with id {AccountId} - Error: {ErrorCode}",
                    account.AccountId, 
                    createSessionResult.ErrorCode
                );
                return ServiceResult<CreatedSession>.Failure(ServiceError.SessionCreationFailed);
            }

            await tx.CommitAsync(CancellationToken.None);


            Session createdSession = createSessionResult.Value;

            CreatedSession result = new(createdSession.SessionId.ToString(), createdSession.ExpiresAt);

            return ServiceResult<CreatedSession>.Success(result);
        }
    }
}