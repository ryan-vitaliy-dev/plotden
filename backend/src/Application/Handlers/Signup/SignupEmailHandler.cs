using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

using Application.Accounts;
using Application.Tokens;
using Application.Tokens.Results;
using Application.Common;
using Application.Resources;
using Application.Email;
using Application.Auth.Results;
using Domain.Common;
using Domain.Accounts;
using Domain.Sessions;
using Domain.Tokens;
using Application.Handlers.Common;

namespace Application.Handlers.Signup
{
    public class SignupEmailHandler(
        ILogger<SignupEmailHandler> logger, 
        AccountService accountService, 
        TokenService tokenService, 
        EmailService emailService,
        TokenEmailHandler tokenEmailHandler,
        IStringLocalizer<SharedResource> localizer)
    {

        private readonly ILogger<SignupEmailHandler> _logger = logger;
        private readonly AccountService _accountService = accountService;
        private readonly EmailService _emailService = emailService;
        private readonly TokenService _tokenService = tokenService;
        private readonly TokenEmailHandler _tokenEmailHandler = tokenEmailHandler;
        private readonly IStringLocalizer<SharedResource> _localizer = localizer;
        

        /// <summary>
        /// Handles account signup by creating the <see cref="Account"/>, initiating a <see cref="Session"/>, generating an email verification token, and sending the token to the user's email.
        /// </summary>
        /// <param name="dto">The Data Transfer Object containing the email and password for the new account.</param>
        /// <param name="clt">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
        /// <returns>
        /// A <see cref="ServiceResult{T}"/> containing an <see cref="AccountSignupEmailResult"/> if the signup succeeds,
        /// or a failure with an appropriate <see cref="ServiceError"/> if any step fails.
        /// </returns>
        public async Task<ServiceResult<SignupEmailResult>> HandleAsync(string email, 
        CancellationToken clt)
        {
            Account? accountToUseForSignup = null;
            DateTimeOffset consistentCreatedAtDateTime = DateTimeOffset.UtcNow;

            ServiceResult<Account> existingAccountCheckResult = await _accountService.FindAccountByEmailAsync(email, clt);
            if(existingAccountCheckResult.IsSuccess)
            { 
                Account existingAccount = existingAccountCheckResult.Value;
                if(existingAccount.VerifiedAt != null)
                {
                    // If account is verified, handle it differently.
                    return await HandleExistingVerifiedAccountAsync(existingAccount, consistentCreatedAtDateTime, clt);
                }
                // Otherwise, use Account, generate Token, construct link, and send email with link
                accountToUseForSignup = existingAccount;
            }
            else if(existingAccountCheckResult.ErrorCode.HasValue && existingAccountCheckResult.ErrorCode != ServiceError.NoAccountFound)
            {
                // Other issue occurred - either invalid email somehow or 
                // TODO: FE sees error like "Unknown error - try again later"
                return ServiceResult<SignupEmailResult>.Failure(existingAccountCheckResult.ErrorCode.Value);
            }

            if(accountToUseForSignup == null)
            {
                ServiceResult<Account> accountCreationResult = await _accountService.CreateAccountAsync(email, consistentCreatedAtDateTime, clt: clt);
                if(accountCreationResult.IsFailure)
                {
                    _logger.LogError("Account creation failed for email {Email}. Error: {ErrorCode}", email, accountCreationResult.ErrorCode);
                    return accountCreationResult.ErrorCode switch
                    {
                        ServiceError.InvalidInput => ServiceResult<SignupEmailResult>.Failure(ServiceError.InvalidInput),
                        ServiceError.OperationCancelled => ServiceResult<SignupEmailResult>.Failure(ServiceError.OperationCancelled),
                        _ => ServiceResult<SignupEmailResult>.Failure(ServiceError.UnknownError)
                    };
                }
                accountToUseForSignup = accountCreationResult.Value;
            }
            // Generate Token, construct link, and send email with link
            // NOTE: The TokenEmailTemplate argument for this call has no affect, it was just a dirty hack fix for now. Will clean up in the future
            return await _tokenEmailHandler.GenerateTokenAndSendEmailAsync(accountToUseForSignup, TokenType.EmailVerification, TokenEmailTemplate.ResumeSignup_Retry, consistentCreatedAtDateTime, clt);
        }


        private async Task<ServiceResult<SignupEmailResult>> HandleExistingVerifiedAccountAsync(Account existingAccount, DateTimeOffset? createdAtOverride, 
        CancellationToken clt)
        {
            if(existingAccount.FinishedSignupAt != null)
            {
                // TODO: Add rate limiting per email address before production
                // e.g. max 3 emails per hour to the same address so they dont get flooded
                
                // Simply warn them that someone else tried to sign up a new Account with the email
                _logger.LogInformation("Signup attempt with email {Email} that already has a verified account. Sending warning email.", existingAccount.Email);
                
                ServiceResult<Unit> emailSendResult = await _emailService.SendAccountExistsEmailAsync(existingAccount.Email, clt);
                if(emailSendResult.IsSuccess)
                {
                    SignupEmailResult result = new(existingAccount.Email);
                    return ServiceResult<SignupEmailResult>.Success(result);
                }
                else
                {
                    return ServiceResult<SignupEmailResult>.Failure(emailSendResult.ErrorCode!.Value);
                }
            }
            else {
                // TODO: This inherently invalidates previous tokens. Double check to make sure this is okay, since it may be possible that someone enters the email in signup while
                // the original person requests recovery in the signin page. very niche edge case, probably will just ignore it since it doesnt result in any invalid states, just is a little confusing for the real owner of the inbox if it occurs
                // Update 5/29/2026 - note sure what I meant by the above comment, will look into it later. For now, I added TokenEmailTemplate to differentiate the two
                // cases where ResumeSignup tokentype is used (e.g. 1. for retrying signup and for recovery. May end up removing retrying signup, not sure if I added it as a user
                // convienence or something)
                return await _tokenEmailHandler.GenerateTokenAndSendEmailAsync(existingAccount, TokenType.ResumeSignup, TokenEmailTemplate.ResumeSignup_Retry, createdAtOverride, clt);
            }
        }
    }
}