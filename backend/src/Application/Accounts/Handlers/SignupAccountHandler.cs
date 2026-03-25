using backend.Application.Accounts;
using backend.Application.Accounts.DTOs;
using backend.Domain.Accounts;
using backend.Infrastructure.Common;

namespace backend.Application.Accounts.Handlers
{
    public class SignupAccountHandler(AccountService accountService)
    {
        private readonly AccountService _accountService = accountService;
        //private readonly SessionService _sessionService = sessionService;
        //private readonly TokenService _tokenService = tokenService;

        // TODO: return some SignupData record class rather than Account since we'll need to return session data etc.
        public async Task<ServiceResult<Account>> HandleAsync(AccountSignupDTO dto, CancellationToken clt)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            {
                return ServiceResult<Account>.Failure(ServiceError.InvalidInput);
            }

            ServiceResult<Account> accountCreationResult = await _accountService.CreateAccountAsync(dto.Email, dto.Password, clt: clt);
            if(accountCreationResult.IsFailure)
            {
                // return accountCreationResult.ErrorCode switch
                // {
                //     ServiceError.InvalidInput => BadRequest(new { message = _localizer["GeneralBadRequest"]}),
                //     ServiceError.OperationCancelled => StatusCode(499),
                //     _ => StatusCode(500, new { message = _localizer["GeneralServerError"]})
                // };
                return ServiceResult<Account>.Failure(ServiceError.UnknownError);
            }

            return ServiceResult<Account>.Success(accountCreationResult.Value);

            // // Create session
            // var session = await _sessionService.CreateSessionAsync(account.Id, ct);

            // if (session == null)
            // {
            //     return ServiceResult.Failure("Failed to create session.");
            // }

            // // Generate token
            // var token = _tokenService.GenerateToken(account.Id);

            // return ServiceResult.Success(token);
        }
    }
}