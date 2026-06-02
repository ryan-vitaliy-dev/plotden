using Application.Accounts;
using Application.Auth;
using Application.Common;
using Domain.Accounts;
using Domain.Common;

namespace Application.Handlers.Signin
{
    public class SigninApplyResetPasswordHandler(AccountService accountService, AuthService authService)
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

            ServiceResult<Unit> resetPasswordResult = await _authService.FinishResetPasswordAsync(account, password, clt);
            if(resetPasswordResult.IsFailure)
            {
                return ServiceResult<Unit>.Failure(resetPasswordResult.ErrorCode!.Value);
            }
            return ServiceResult<Unit>.Success(Unit.Value);
        }
    }
}