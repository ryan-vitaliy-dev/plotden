using Application.Accounts;
using Application.Common;
using Application.Sessions;
using Application.Tokens;
using Domain.Accounts;
using Domain.Common;
using Domain.Sessions;
using Domain.Tokens;

namespace Application.Handlers.Signup
{
    public class SignupPasswordHandler(AccountService accountService)
    {
        private readonly AccountService _accountService = accountService;

        public async Task<ServiceResult<Unit>> HandleAsync(Guid accountId, string password, CancellationToken clt)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return ServiceResult<Unit>.Failure(ServiceError.InvalidInput);
            }

            ServiceResult<Account> getAccountResult = await _accountService.FindAccountByIdAsync(accountId, clt);
            if(getAccountResult.IsFailure)
            {
                // TODO: check if this will ever even be called, controller might gaurantee this is safe, not sure yet
                return ServiceResult<Unit>.Failure(ServiceError.InvalidInput);
            }
            Account account = getAccountResult.Value;

            if(account.VerifiedAt == null)
            {
                // Account should be verified before setting password, otherwise something went wrong in the flow
                return ServiceResult<Unit>.Failure(ServiceError.AccountNotVerified);
            }

            if(account.PasswordHash != null)
            {
                // Account should be verified and not have a password already, otherwise something went wrong in the flow
                return ServiceResult<Unit>.Failure(ServiceError.PasswordAlreadySet);
            }

            ServiceResult<Unit> passwordSetResult = await _accountService.SetAccountPasswordAsync(account, password, clt);
            if(passwordSetResult.IsFailure)
            {
                return ServiceResult<Unit>.Failure(passwordSetResult.ErrorCode!.Value);
            }
            return ServiceResult<Unit>.Success(Unit.Value);
        }
    }
}