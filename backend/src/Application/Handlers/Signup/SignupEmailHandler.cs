using backend.API.DTOs.Accounts;

using backend.Application.Accounts;
using backend.Application.Accounts.DTOs;
using backend.Application.Tokens;
using backend.Application.Tokens.DTOs;

using backend.Infrastructure.Common;
using backend.Infrastructure.Email;

using backend.Domain.Accounts;
using backend.Domain.Sessions;
using backend.Domain.Tokens;
using backend.Application.Common;
using Microsoft.Extensions.Localization;
using backend.Resources;
using backend.Application.Email;

namespace backend.Application.Handlers.Signup
{
    public class SignupEmailHandler(
        ILogger<SignupEmailHandler> logger, 
        AccountService accountService, 
        TokenService tokenService, 
        EmailService emailService,
        IStringLocalizer<SharedResource> localizer)
    {

        private readonly ILogger<SignupEmailHandler> _logger = logger;
        private readonly AccountService _accountService = accountService;
        private readonly EmailService _emailService = emailService;
        private readonly TokenService _tokenService = tokenService;
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
        public async Task<ServiceResult<AccountSignupEmailResult>> HandleAsync(AccountSignupEmailDTO dto, 
        CancellationToken clt)
        {
            Account? accountToUseForSignup = null;
            DateTimeOffset consistentCreatedAtDateTime = DateTimeOffset.UtcNow;

            ServiceResult<Account> existingAccountCheckResult = await _accountService.FindAccountByEmailAsync(dto.Email, clt);
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
                return ServiceResult<AccountSignupEmailResult>.Failure(existingAccountCheckResult.ErrorCode.Value);
            }

            if(accountToUseForSignup == null)
            {
                ServiceResult<Account> accountCreationResult = await _accountService.CreateAccountAsync(dto.Email, consistentCreatedAtDateTime, clt: clt);
                if(accountCreationResult.IsFailure)
                {
                    _logger.LogError("Account creation failed for email {Email}. Error: {ErrorCode}", dto.Email, accountCreationResult.ErrorCode);
                    return accountCreationResult.ErrorCode switch
                    {
                        ServiceError.InvalidInput => ServiceResult<AccountSignupEmailResult>.Failure(ServiceError.InvalidInput),
                        ServiceError.OperationCancelled => ServiceResult<AccountSignupEmailResult>.Failure(ServiceError.OperationCancelled),
                        _ => ServiceResult<AccountSignupEmailResult>.Failure(ServiceError.UnknownError)
                    };
                }
                accountToUseForSignup = accountCreationResult.Value;
            }
            // Generate Token, construct link, and send email with link
            return await GenerateTokenAndSendEmail(accountToUseForSignup, TokenType.EmailVerification, consistentCreatedAtDateTime, clt);
        }


        private async Task<ServiceResult<AccountSignupEmailResult>> HandleExistingVerifiedAccountAsync(Account existingAccount, DateTimeOffset? createdAtOverride, 
        CancellationToken clt)
        {
            if(existingAccount.FinishedSignupAt != null)
            {
                // Simply warn them that someone else tried to sign up a new Account with the email
                _logger.LogInformation("Signup attempt with email {Email} that already has a verified account. Sending warning email.", existingAccount.Email);
                
                ServiceResult<Unit> emailSendResult = await _emailService.SendAccountExistsEmailAsync(existingAccount.Email, clt);
                if(emailSendResult.IsSuccess)
                {
                    AccountSignupEmailResult result = new(existingAccount.Email);
                    return ServiceResult<AccountSignupEmailResult>.Success(result);
                }
                else
                {
                    return ServiceResult<AccountSignupEmailResult>.Failure(emailSendResult.ErrorCode!.Value);
                }
            }
            else {
                return await GenerateTokenAndSendEmail(existingAccount, TokenType.ResumeSignup, createdAtOverride, clt);
            }
        }



        private async Task<ServiceResult<AccountSignupEmailResult>> GenerateTokenAndSendEmail(Account account, TokenType tokenType, DateTimeOffset? createdAtOverride, 
        CancellationToken clt)
        {
            // Invalidate old tokens
            ServiceResult<Unit> invalidateTokenResult = await _tokenService.InvalidateTokenAsync(account.AccountId, tokenType, clt);
            if(invalidateTokenResult.IsFailure)
            {
                // Do not issue a new token.
                return invalidateTokenResult.ErrorCode switch
                {
                    ServiceError.InvalidInput => ServiceResult<AccountSignupEmailResult>.Failure(ServiceError.InvalidInput),
                    ServiceError.OperationCancelled => ServiceResult<AccountSignupEmailResult>.Failure(ServiceError.OperationCancelled),
                    _ => ServiceResult<AccountSignupEmailResult>.Failure(ServiceError.UnknownError)
                };
            }

            ServiceResult<TokenCreationResult> tokenCreationResult = await _tokenService.CreateTokenAsync(account.AccountId, tokenType, createdAtOverride, clt);
            if(tokenCreationResult.IsFailure)
            {
                _logger.LogWarning("{TokenType} token creation failed for email {Email}. Error: {ErrorCode}", tokenType.ToString(), account.Email, tokenCreationResult.ErrorCode);
                // Note: If this fails, it doesnt matter if we report it since we have an avenue for them to resend it anyways(?)
                return tokenCreationResult.ErrorCode switch
                {
                    ServiceError.InvalidInput => ServiceResult<AccountSignupEmailResult>.Failure(ServiceError.InvalidInput),
                    ServiceError.OperationCancelled => ServiceResult<AccountSignupEmailResult>.Failure(ServiceError.OperationCancelled),
                    _ => ServiceResult<AccountSignupEmailResult>.Failure(ServiceError.UnknownError)
                };
            }
            TokenCreationResult createdToken = tokenCreationResult.Value;

            ServiceResult<Unit> emailSendResult;
            if(tokenType == TokenType.EmailVerification)
            {
                emailSendResult = await _emailService.SendVerificationEmailAsync(account.Email, createdToken.TokenRaw, clt);
            }
            else if(tokenType == TokenType.ResumeSignup)
            {
                emailSendResult = await _emailService.SendAccountExistsResumeSignupEmailAsync(account.Email, createdToken.TokenRaw, clt);
            }
            else
            {
                return ServiceResult<AccountSignupEmailResult>.Failure(ServiceError.UnknownError);
            }

            if(emailSendResult.IsSuccess)
            {
                AccountSignupEmailResult result = new(account.Email);
                return ServiceResult<AccountSignupEmailResult>.Success(result);
            }
            else
            {
                return ServiceResult<AccountSignupEmailResult>.Failure(emailSendResult.ErrorCode!.Value);
            }
        }
    }
}