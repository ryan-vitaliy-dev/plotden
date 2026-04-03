using Application.Accounts;
using Application.Sessions;
using Application.Tokens;
using Domain.Accounts;
using Domain.Sessions;
using Domain.Tokens;

namespace Application.Handlers.Signup
{
    public class SignupPasswordHandler(AccountService accountService)
    {
        private readonly AccountService _accountService = accountService;

        // TODO

        //public async Task<ServiceResult<AccountSignupResult>> HandleAsync(AccountSignupEmailDTO dto, string ipAddress, string userAgent, CancellationToken clt)
        //{
            // if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            // {
            //     return ServiceResult<AccountSignupResult>.Failure(ServiceError.InvalidInput);
            // }
            // if(clt.IsCancellationRequested)
            // {
            //     return ServiceResult<AccountSignupResult>.Failure(ServiceError.OperationCancelled);
            // }
            // DateTimeOffset consistentCreatedAtDateTime = DateTimeOffset.UtcNow;

            // // Create Account
            // ServiceResult<Account> accountCreationResult = await _accountService.CreateAccountAsync(dto.Email, dto.Password, consistentCreatedAtDateTime, clt: clt);
            // if(accountCreationResult.IsFailure)
            // {
            //     return accountCreationResult.ErrorCode switch
            //     {
            //         ServiceError.InvalidInput => ServiceResult<AccountSignupResult>.Failure(ServiceError.InvalidInput),
            //         ServiceError.OperationCancelled => ServiceResult<AccountSignupResult>.Failure(ServiceError.OperationCancelled),
            //         _ => ServiceResult<AccountSignupResult>.Failure(ServiceError.UnknownError)
            //     };
            // }
            // Account createdAccount = accountCreationResult.Value;

            // // Create Session
            // ServiceResult<Session> sessionCreationResult = await _sessionService.CreateSessionAsync(createdAccount.AccountId, ipAddress, userAgent, consistentCreatedAtDateTime, clt);
            // if(sessionCreationResult.IsFailure)
            // {
            //     return sessionCreationResult.ErrorCode switch
            //     {
            //         ServiceError.InvalidInput => ServiceResult<AccountSignupResult>.Failure(ServiceError.InvalidInput),
            //         ServiceError.OperationCancelled => ServiceResult<AccountSignupResult>.Failure(ServiceError.OperationCancelled),
            //         _ => ServiceResult<AccountSignupResult>.Failure(ServiceError.UnknownError)
            //     };
            // }
            // Session createdSession = sessionCreationResult.Value;

            // // Create Email Verification Token
            // ServiceResult<Token> emailVerificationTokenCreationResult = await _tokenService.CreateTokenAsync(createdAccount.AccountId, TokenType.EmailVerification, consistentCreatedAtDateTime, clt);
            // if(emailVerificationTokenCreationResult.IsFailure)
            // {
            //     return emailVerificationTokenCreationResult.ErrorCode switch
            //     {
            //         ServiceError.InvalidInput => ServiceResult<AccountSignupResult>.Failure(ServiceError.InvalidInput),
            //         ServiceError.OperationCancelled => ServiceResult<AccountSignupResult>.Failure(ServiceError.OperationCancelled),
            //         _ => ServiceResult<AccountSignupResult>.Failure(ServiceError.UnknownError)
            //     };
            // }
            // Token createdToken = emailVerificationTokenCreationResult.Value;

            // // TODO: send info

            // AccountSignupResult accountSignupResult = new(accountCreationResult.Value.Email, createdSession.SessionId, createdSession.ExpiresAt);
            // return ServiceResult<AccountSignupResult>.Success(accountSignupResult);
        //}
    }
}