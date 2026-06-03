using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using Application.Accounts;
using Application.Common;
using Application.Sessions;
using Domain.Accounts;
using Domain.Common;

namespace Application.Handlers.Accounts
{
    public class UpdatePasswordHandler(AccountService accountService, SessionService sessionService, ILogger<UpdatePasswordHandler> logger)
    {
        private readonly AccountService _accountService = accountService;
        private readonly SessionService _sessionService = sessionService;
        private readonly ILogger<UpdatePasswordHandler> _logger = logger;
        
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

            ServiceResult<Unit> updatePasswordResult = await _accountService.SetAccountPasswordAsync(account, newPassword, clt);
            if(updatePasswordResult.IsFailure)
            {
                return ServiceResult<Unit>.Failure(updatePasswordResult.ErrorCode!.Value);
            }

            ServiceResult<Unit> invalidateAllOtherSessionsResult = await _sessionService.InvalidAllSessionsExceptAsync(account.AccountId, currentSessionId, clt);
            if(invalidateAllOtherSessionsResult.IsFailure)
            {
                return ServiceResult<Unit>.Failure(updatePasswordResult.ErrorCode!.Value);
            }

            return ServiceResult<Unit>.Success(Unit.Value);
        }
    }
}