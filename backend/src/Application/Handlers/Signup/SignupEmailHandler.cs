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

namespace backend.Application.Handlers.Signup
{
    public class SignupEmailHandler(
        ILogger<SignupEmailHandler> logger, 
        AccountService accountService, 
        TokenService tokenService, 
        IEmailSender emailSender,
        IStringLocalizer<SharedResource> localizer)
    {

        private readonly ILogger<SignupEmailHandler> _logger = logger;
        private readonly AccountService _accountService = accountService;
        private readonly IEmailSender _emailSender = emailSender;
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
        public async Task<ServiceResult<AccountSignupEmailResult>> HandleAsync(AccountSignupEmailDTO dto, CancellationToken clt)
        {
            DateTimeOffset consistentCreatedAtDateTime = DateTimeOffset.UtcNow;

            ServiceResult<Account> existingAccountCheckResult = await _accountService.FindAccountByEmailAsync(dto.Email, clt);
            if(existingAccountCheckResult.IsSuccess)
            {
                Account foundAccount = existingAccountCheckResult.Value;
                return await HandleExistingAccountAsync(foundAccount, consistentCreatedAtDateTime, clt);
            }

            if(existingAccountCheckResult.ErrorCode.HasValue && existingAccountCheckResult.ErrorCode != ServiceError.NoAccountFound)
            {
                // Other issue occurred - either invalid email somehow or 
                // TODO: FE sees error like "Unknown error - try again later"
                return ServiceResult<AccountSignupEmailResult>.Failure(existingAccountCheckResult.ErrorCode.Value);
            }

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
            Account createdAccount = accountCreationResult.Value;
            return await GenerateTokenAndSendVerificationEmail(createdAccount, consistentCreatedAtDateTime, clt);
        }


        private async Task<ServiceResult<AccountSignupEmailResult>> HandleExistingAccountAsync(Account existingAccount, DateTimeOffset? createdAtOverride, CancellationToken clt)
        {
            if(existingAccount.HasVerifiedEmail)
            {
                // Send warning email to existing account owner and pretend-prompt client that an email was sent.
                _logger.LogInformation("Signup attempt with email {Email} that already has a verified account. Sending warning email.", existingAccount.Email);
                string warningEmailBody = EmailTemplateLoader.LoadTemplate("AccountAlreadyExists.html");

                bool warningEmailSent = await _emailSender.SendAsync(new EmailMessage(existingAccount.Email, _localizer["Email_SubjectAccountAlreadyExists"].Value, warningEmailBody), clt);
                if(warningEmailSent)
                {
                    AccountSignupEmailResult result = new(existingAccount.Email);
                    return ServiceResult<AccountSignupEmailResult>.Success(result);
                }
                else
                {
                    _logger.LogWarning("Warning email failed to be sent to email {Email}.", existingAccount.Email);
                    return ServiceResult<AccountSignupEmailResult>.Failure(ServiceError.UnknownError);
                }
            }
            return await GenerateTokenAndSendVerificationEmail(existingAccount, createdAtOverride, clt);
        }


        private async Task<ServiceResult<AccountSignupEmailResult>> GenerateTokenAndSendVerificationEmail(Account account, DateTimeOffset? createdAtOverride, CancellationToken clt)
        {
            // Invalidate old tokens
            ServiceResult<Unit> invalidateTokenResult = await _tokenService.InvalidateTokenAsync(account.AccountId, TokenType.EmailVerification, clt);
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
            
            ServiceResult<TokenCreationResult> verificationEmailTokenCreationResult = await _tokenService.CreateTokenAsync(account.AccountId, TokenType.EmailVerification, createdAtOverride, clt);
            if(verificationEmailTokenCreationResult.IsFailure)
            {
                _logger.LogWarning("Verification token creation failed for email {Email}. Error: {ErrorCode}", account.Email, verificationEmailTokenCreationResult.ErrorCode);
                // Note: If this fails, it doesnt matter if we report it since we have an avenue for them to resend it anyways.
                return verificationEmailTokenCreationResult.ErrorCode switch
                {
                    ServiceError.InvalidInput => ServiceResult<AccountSignupEmailResult>.Failure(ServiceError.InvalidInput),
                    ServiceError.OperationCancelled => ServiceResult<AccountSignupEmailResult>.Failure(ServiceError.OperationCancelled),
                    _ => ServiceResult<AccountSignupEmailResult>.Failure(ServiceError.UnknownError)
                };
            }
            TokenCreationResult createdToken = verificationEmailTokenCreationResult.Value;

            // Note: ran into an issue here because we dont store the raw token aka pre-hashed token value, so we cant recycle it.
            // TokenCreationResult createdOrRecycledToken;
            // ServiceResult<Token> existingTokenCheckResult = await _tokenService.FindActiveTokenByAccountAndTypeAsync(account.AccountId, TokenType.EmailVerification, clt);
            // if(existingTokenCheckResult.IsFailure)
            // {
            //     // TODO: Invalidate expired token, may need to update FindActiveTokenByAccountAndTypeAsync return to support that
            //     // No existing active token found so create a new one.
            //     ServiceResult<TokenCreationResult> verificationEmailTokenCreationResult = await _tokenService.CreateTokenAsync(account.AccountId, TokenType.EmailVerification, createdAtOverride, clt);
            //     if(verificationEmailTokenCreationResult.IsFailure)
            //     {
            //         _logger.LogWarning("Verification token creation failed for email {Email}. Error: {ErrorCode}", account.Email, verificationEmailTokenCreationResult.ErrorCode);
            //         // Note: If this fails, it doesnt matter if we report it since we have an avenue for them to resend it anyways.
            //         return verificationEmailTokenCreationResult.ErrorCode switch
            //         {
            //             ServiceError.InvalidInput => ServiceResult<AccountSignupEmailResult>.Failure(ServiceError.InvalidInput),
            //             ServiceError.OperationCancelled => ServiceResult<AccountSignupEmailResult>.Failure(ServiceError.OperationCancelled),
            //             _ => ServiceResult<AccountSignupEmailResult>.Failure(ServiceError.UnknownError)
            //         };
            //     }
            //     createdOrRecycledToken = verificationEmailTokenCreationResult.Value;
            // }
            // else
            // {
            //     // Recycle existing active token
            //     Token existingToken = existingTokenCheckResult.Value;
            //     createdOrRecycledToken = new(existingToken.)
            // }

            string verificationEmailBody = EmailTemplateLoader.LoadTemplate("SignupVerificationLink.html", new Dictionary<string, string>
            {
                ["VerificationLink"] = "plotden.com/verify?token=" + createdToken.TokenRaw
            });
            bool verificationEmailSent = await _emailSender.SendAsync(new EmailMessage(account.Email, _localizer["Email_SubjectVerifyEmailAddress"].Value, verificationEmailBody), clt);
            if(verificationEmailSent)
            {
                AccountSignupEmailResult result = new(account.Email);
                return ServiceResult<AccountSignupEmailResult>.Success(result);
            }
            else
            {
                _logger.LogWarning("Verification email failed to be sent to email {Email}.", account.Email);
                return ServiceResult<AccountSignupEmailResult>.Failure(ServiceError.UnknownError);
            }
        }
    }
}