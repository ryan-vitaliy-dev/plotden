using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using Application.Accounts;
using Application.Common;
using Application.Sessions;
using Domain.Accounts;
using Domain.Common;
using Application.Common.Interfaces;

namespace Application.Handlers.Accounts
{
    public class UpdatePasswordHandler(
        ILogger<UpdatePasswordHandler> logger,
        IUnitOfWork unitOfWork,
        AccountService accountService, 
        SessionService sessionService 
    )
    {
        private readonly ILogger<UpdatePasswordHandler> _logger = logger;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        private readonly AccountService _accountService = accountService;
        private readonly SessionService _sessionService = sessionService;
        
        private readonly PasswordHasher<Account> _passwordHasher = new();

        public async Task<ServiceResult<Unit>> HandleAsync(Guid accountId, Guid currentSessionId, string currentPassword, string newPassword, CancellationToken clt)
        {
            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                return ServiceResult<Unit>.Failure(ServiceError.InvalidInput);
            }
            
            ServiceResult<Account> existingAccountCheckResult = await _accountService.FindAccountByIdAsync(accountId, clt);
            if(existingAccountCheckResult.IsFailure)
            {
                return ServiceResult<Unit>.Failure(ServiceError.NoAccountFound);
            }
            Account account = existingAccountCheckResult.Value;

            if(account.PasswordHash == null)
            {
                return ServiceResult<Unit>.Failure(ServiceError.PasswordNotSet);
            }

            PasswordVerificationResult passwordVerificationResult = _passwordHasher.VerifyHashedPassword(account, account.PasswordHash, currentPassword);
            if(passwordVerificationResult == PasswordVerificationResult.Failed)
            {
                return ServiceResult<Unit>.Failure(ServiceError.InvalidCredentials);
            }
            // TODO: handle rehash stuff later if needed


            await using var tx = await _unitOfWork.BeginTransactionAsync(clt);
            
            ServiceResult<Unit> updatePasswordResult = await _accountService.SetPasswordAsync(account, newPassword, clt);
            if(updatePasswordResult.IsFailure)
            {
                await tx.RollbackAsync(CancellationToken.None);
                return ServiceResult<Unit>.Failure(updatePasswordResult.ErrorCode!.Value); // TODO: See if this is ok
            }


            ServiceResult<Unit> invalidateOtherSessionsResult = await _sessionService.InvalidateOtherSessionsAsync(account.AccountId, currentSessionId, clt);
            if(invalidateOtherSessionsResult.IsFailure)
            {
                await tx.RollbackAsync(CancellationToken.None);
                return ServiceResult<Unit>.Failure(updatePasswordResult.ErrorCode!.Value); // TODO: See if this is ok
            }

            await tx.CommitAsync(clt);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
    }
}