using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using Application.Accounts;
using Application.Sessions;
using Domain.Accounts;
using Domain.Common;
using Domain.Sessions;
using Application.Common;
using Application.Sessions.Results;

namespace Application.Handlers.Auth
{
    public class SigninHandler(
        ILogger<SigninHandler> logger,
        AccountService accountService, 
        SessionService sessionService
    )
    {
        private readonly ILogger<SigninHandler> _logger = logger;

        private readonly AccountService _accountService = accountService;
        private readonly SessionService _sessionService = sessionService;

        private readonly PasswordHasher<Account> _passwordHasher = new();

        public async Task<ServiceResult<CreatedSession>> HandleAsync(string email, string password, ClientInfo clientInfo, CancellationToken clt)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return ServiceResult<CreatedSession>.Failure(ServiceError.InvalidInput);
            }

            ServiceResult<Account> findAccountResult = await _accountService.FindAccountByEmailAsync(email, clt);
            Account account = findAccountResult.IsFailure 
                ? new Account()
                : findAccountResult.Value;

            PasswordVerificationResult passwordVerificationResult = _passwordHasher.VerifyHashedPassword(account, account.PasswordHash ?? string.Empty, password);
            if(findAccountResult.IsFailure || passwordVerificationResult == PasswordVerificationResult.Failed)
            {
                return ServiceResult<CreatedSession>.Failure(ServiceError.InvalidCredentials);
            }
            // TODO: handle rehash stuff later if needed


            ServiceResult<Session> createSessionResult = await _sessionService.CreateSessionAsync(
                account.AccountId, 
                SessionType.Standard, 
                clientInfo, 
                null, 
                clt
            );
            if(createSessionResult.IsFailure)
            {
                // If session creation fails, show error to user and tell them to try link again
                _logger.LogError(
                    "Failed to create session for account id {AccountId} - Error: {ErrorCode}", 
                    account.AccountId, 
                    createSessionResult.ErrorCode
                );
                return ServiceResult<CreatedSession>.Failure(ServiceError.SessionCreationFailed);
            }
            Session createdSession = createSessionResult.Value;

            CreatedSession result = new(createdSession.SessionId.ToString(), createdSession.ExpiresAt);

            return ServiceResult<CreatedSession>.Success(result);
        }
    }
}