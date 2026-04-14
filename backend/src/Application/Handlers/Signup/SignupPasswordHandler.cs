using Application.Accounts;
using Application.Auth;
using Application.Common;
using Domain.Accounts;
using Domain.Common;

namespace Application.Handlers.Signup
{
    public class SignupPasswordHandler(AccountService accountService, AuthService authService)
    {
        private readonly AccountService _accountService = accountService;
        private readonly AuthService _authService = authService;

        public async Task<ServiceResult<Unit>> HandleAsync(Guid accountId, string password, CancellationToken clt)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return ServiceResult<Unit>.Failure(ServiceError.InvalidInput);
            }

            ServiceResult<Account> findAccountResult = await _accountService.FindAccountByIdAsync(accountId, clt);
            if(findAccountResult.IsFailure)
            {
                return ServiceResult<Unit>.Failure(ServiceError.InvalidInput);
            }
            Account account = findAccountResult.Value;
            
            bool isNotVerified = account.VerifiedAt == null;
            bool alreadySetPassword = account.PasswordHash != null || account.FinishedSignupAt != null;

            if(isNotVerified)
            {
                return ServiceResult<Unit>.Failure(ServiceError.AccountNotVerified);
            }

            if(alreadySetPassword)
            {
                return ServiceResult<Unit>.Failure(ServiceError.PasswordAlreadySet);
            }

            ServiceResult<Unit> finishAccountSignupResult = await _authService.FinishAccountSignupAsync(account, password, clt);
            if(finishAccountSignupResult.IsFailure)
            {
                return ServiceResult<Unit>.Failure(finishAccountSignupResult.ErrorCode!.Value);
            }
            return ServiceResult<Unit>.Success(Unit.Value);
        }
    }
}