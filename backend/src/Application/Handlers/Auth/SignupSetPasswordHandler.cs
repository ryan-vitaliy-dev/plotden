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
    public class SignupSetPasswordHandler(
        ILogger<SignupSetPasswordHandler> logger,
        IUnitOfWork unitOfWork,
        AccountService accountService, 
        SessionService sessionService
    )
    {
        private readonly ILogger<SignupSetPasswordHandler> _logger = logger;
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
                return ServiceResult<CreatedSession>.Failure(ServiceError.InvalidInput);
            }
            Account account = findAccountResult.Value;
            
            bool accountIsNotVerified = account.VerifiedAt == null;
            bool accountAlreadySetPassword = account.PasswordHash != null;

            if(accountIsNotVerified) return ServiceResult<CreatedSession>.Failure(ServiceError.AccountNotVerified);
            if(accountAlreadySetPassword) return ServiceResult<CreatedSession>.Failure(ServiceError.PasswordAlreadySet);


            await using var tx = await _unitOfWork.BeginTransactionAsync(clt);

            ServiceResult<Unit> setAccountPasswordResult = await _accountService.SetPasswordAsync(account, password, clt);
            if(setAccountPasswordResult.IsFailure)
            {
                await tx.RollbackAsync(CancellationToken.None);
                _logger.LogError(
                    "Failed to set password for account with id {AccountId} - Error: {ErrorCode}", 
                    account.AccountId, 
                    setAccountPasswordResult.ErrorCode!.Value
                );
                return ServiceResult<CreatedSession>.Failure(setAccountPasswordResult.ErrorCode!.Value); // TODO: Check if this is ok
            }


            ServiceResult<Unit> markAccountFinishedSignupResult = await _accountService.MarkFinishedSignupAsync(account, clt);
            if(markAccountFinishedSignupResult.IsFailure)
            {
                await tx.RollbackAsync(CancellationToken.None);
                _logger.LogError(
                    "Failed to mark finished signup for account with id {AccountId} - Error: {ErrorCode}", 
                    account.AccountId, 
                    markAccountFinishedSignupResult.ErrorCode!.Value
                );
                return ServiceResult<CreatedSession>.Failure(markAccountFinishedSignupResult.ErrorCode!.Value); // TODO: Check if this is ok
            }


            ServiceResult<Unit> invalidateAccountSessionsResult = await _sessionService.InvalidateSessionsAsync(account.AccountId, clt);
            if(invalidateAccountSessionsResult.IsFailure)
            {
                await tx.RollbackAsync(CancellationToken.None);
                _logger.LogError(
                    "Failed to invalidate sessions for account with id {AccountId} - Error: {ErrorCode}",
                    account.AccountId, 
                    invalidateAccountSessionsResult.ErrorCode!.Value
                );
                return ServiceResult<CreatedSession>.Failure(invalidateAccountSessionsResult.ErrorCode!.Value); // TODO: Check if this is ok
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
                    createSessionResult.ErrorCode!.Value
                );
                return ServiceResult<CreatedSession>.Failure(ServiceError.SessionCreationFailed);
            }

            await tx.CommitAsync(clt);


            Session session = createSessionResult.Value;

            CreatedSession createdSessionResponse = new(session.SessionId.ToString(), session.ExpiresAt);

            return ServiceResult<CreatedSession>.Success(createdSessionResponse);
        }
    }
}