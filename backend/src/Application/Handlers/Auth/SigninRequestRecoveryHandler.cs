using Microsoft.Extensions.Logging;

using Application.Accounts;
using Application.Common;
using Application.Handlers.Common;
using Application.Sessions;
using Domain.Accounts;
using Domain.Common;
using Domain.Tokens;

namespace Application.Handlers.Auth
{
    public class SigninRequestRecoveryHandler(AccountService accountService, SessionService sessionService, TokenEmailHandler tokenEmailHandler, ILogger<SigninRequestRecoveryHandler> logger)
    {
        private readonly AccountService _accountService = accountService;
        private readonly SessionService _sessionService = sessionService;
        private readonly TokenEmailHandler _tokenEmailHandler = tokenEmailHandler;
        private readonly ILogger<SigninRequestRecoveryHandler> _logger = logger;

        public async Task<ServiceResult<Unit>> HandleAsync(string email, CancellationToken clt)
        {
            ServiceResult<Account> existingAccountCheckResult = await _accountService.FindAccountByEmailAsync(email, clt);
            if(existingAccountCheckResult.IsFailure)
            {
                // If no account found, return success anyways. Still technically vulnerable to timing attacks but we can rate limit
                return ServiceResult<Unit>.Failure(ServiceError.NoAccountFound);
            }
            Account account = existingAccountCheckResult.Value;

            bool hasVerified = account.VerifiedAt != null;
            bool hasFinishedSignup = account.FinishedSignupAt != null;

            if(hasFinishedSignup)
            {
                ServiceResult<Unit> generatePasswordResetEmailResult = await HandlePasswordResetAsync(account, clt);
                if(generatePasswordResetEmailResult.IsFailure)
                {
                    // TODO: See if this is OK.
                    return ServiceResult<Unit>.Failure(generatePasswordResetEmailResult.ErrorCode!.Value);
                }
                return ServiceResult<Unit>.Success(Unit.Value);
            }

            if(hasVerified)
            {
                ServiceResult<Unit> generateResumeSignupEmailResult = await HandleResumeSignupAsync(account, clt);
                if(generateResumeSignupEmailResult.IsFailure)
                {
                    // TODO: See if this is OK.
                    return ServiceResult<Unit>.Failure(generateResumeSignupEmailResult.ErrorCode!.Value);
                }
                return ServiceResult<Unit>.Success(Unit.Value);
            }

            // if not verified, cant do much about account recovery. The account technically exists but since it hasnt been verified and hasnt set a password the most
            // we'd be able to do is send another verification email. This however is already communicated in the original verification email that gets sent, e.g. "If the link above expires, enter your email on the sign-up page to receive a new verification link."
            // Therefore, we can probably just return success with "We sent an email to xyz with instructions..."
            return ServiceResult<Unit>.Failure(ServiceError.AccountNotVerified);
        }

        public async Task<ServiceResult<Unit>> HandlePasswordResetAsync(Account account, CancellationToken clt)
        {
            ServiceResult<Unit> generateAndSendResetPasswordEmailResult = await _tokenEmailHandler.GenerateTokenAndSendEmailAsync(
                account, 
                TokenType.PasswordReset,
                TokenEmailTemplate.ResumeSignup_Recovery, // Still part of dirty hack, is unused here
                null, 
                clt
            );
            if(generateAndSendResetPasswordEmailResult.IsFailure)
            {
                return ServiceResult<Unit>.Failure(ServiceError.UnknownError);
            }
            return ServiceResult<Unit>.Success(Unit.Value);
        }

        public async Task<ServiceResult<Unit>> HandleResumeSignupAsync(Account account, CancellationToken clt)
        {
            ServiceResult<Unit> generateAndSendResumeSignupEmailResult = await _tokenEmailHandler.GenerateTokenAndSendEmailAsync(
                account, 
                TokenType.ResumeSignup,
                TokenEmailTemplate.ResumeSignup_Recovery, 
                null, 
                clt
            );
            if(generateAndSendResumeSignupEmailResult.IsFailure)
            {
                return ServiceResult<Unit>.Failure(ServiceError.UnknownError);
            }
            return ServiceResult<Unit>.Success(Unit.Value);
        }
    }
}