using backend.API.DTOs.Accounts;
using backend.Application.Accounts;
using backend.Application.Accounts.DTOs;
using backend.Application.Sessions;
using backend.Domain.Accounts;
using backend.Domain.Sessions;
using backend.Infrastructure.Common;

namespace backend.Application.Handlers
{
    public class SignupAccountHandler(AccountService accountService, SessionService sessionService)
    {
        private readonly AccountService _accountService = accountService;
        private readonly SessionService _sessionService = sessionService;

        //private readonly TokenService _tokenService = tokenService;
        

        /// <summary>
        /// Handles account signup by creating the <see cref="Account"/>, initiating a <see cref="Session"/>, generating an email verification token, and sending the token to the user's email.
        /// </summary>
        /// <param name="dto">The Data Transfer Object containing the email and password for the new account.</param>
        /// <param name="ipAddress">The IP address of the client making the signup request.</param>
        /// <param name="userAgent">The user agent string of the client making the signup request.</param>
        /// <param name="clt">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
        /// <returns>
        /// A <see cref="ServiceResult{T}"/> containing an <see cref="AccountSignupResult"/> if the signup succeeds,
        /// or a failure with an appropriate <see cref="ServiceError"/> if any step fails.
        /// </returns>
        public async Task<ServiceResult<AccountSignupResult>> HandleAsync(AccountSignupDTO dto, string ipAddress, string userAgent, CancellationToken clt)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            {
                return ServiceResult<AccountSignupResult>.Failure(ServiceError.InvalidInput);
            }

            if(clt.IsCancellationRequested)
            {
                return ServiceResult<AccountSignupResult>.Failure(ServiceError.OperationCancelled);
            }

            DateTime consistentCreatedAtDateTime = DateTime.UtcNow;

            ServiceResult<Account> accountCreationResult = await _accountService.CreateAccountAsync(dto.Email, dto.Password, consistentCreatedAtDateTime, clt: clt);
            if(accountCreationResult.IsFailure)
            {
                return accountCreationResult.ErrorCode switch
                {
                    ServiceError.InvalidInput => ServiceResult<AccountSignupResult>.Failure(ServiceError.InvalidInput),
                    ServiceError.OperationCancelled => ServiceResult<AccountSignupResult>.Failure(ServiceError.OperationCancelled),
                    _ => ServiceResult<AccountSignupResult>.Failure(ServiceError.UnknownError)
                };
            }
            Account createdAccount = accountCreationResult.Value;

            ServiceResult<Session> sessionCreationResult = await _sessionService.CreateSessionAsync(createdAccount.AccountId, ipAddress, userAgent, consistentCreatedAtDateTime, clt);
            if(sessionCreationResult.IsFailure)
            {
                return sessionCreationResult.ErrorCode switch
                {
                    ServiceError.InvalidInput => ServiceResult<AccountSignupResult>.Failure(ServiceError.InvalidInput),
                    ServiceError.OperationCancelled => ServiceResult<AccountSignupResult>.Failure(ServiceError.OperationCancelled),
                    _ => ServiceResult<AccountSignupResult>.Failure(ServiceError.UnknownError)
                };
            }
            Session createdSession = sessionCreationResult.Value;

            // TODO: Generate verification token and send email (if appropriate)

            AccountSignupResult accountSignupResult = new(accountCreationResult.Value.Email, createdSession.SessionId, createdSession.ExpiresAt);

            return ServiceResult<AccountSignupResult>.Success(accountSignupResult);
        }
    }
}