using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

using Application.Accounts;
using Application.Tokens;
using Application.Common;
using Application.Resources;
using Application.Email;
using Domain.Common;
using Domain.Accounts;
using Domain.Tokens;
using Application.Tokens.Results;
using Application.Common.Interfaces;

namespace Application.Handlers.Auth
{
    public class SignupRequestEmailHandler(
        ILogger<SignupRequestEmailHandler> logger, 
        IStringLocalizer<SharedResource> localizer,
        IUnitOfWork unitOfWork,
        AccountService accountService, 
        TokenService tokenService, 
        EmailService emailService
    )
    {

        private readonly ILogger<SignupRequestEmailHandler> _logger = logger;
        private readonly IStringLocalizer<SharedResource> _localizer = localizer;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        private readonly AccountService _accountService = accountService;
        private readonly EmailService _emailService = emailService;
        private readonly TokenService _tokenService = tokenService;

        
        public async Task<ServiceResult<Unit>> HandleAsync(string email, CancellationToken clt)
        {
            DateTimeOffset consistentCreatedAtDateTime = DateTimeOffset.UtcNow;
            ServiceResult<Account> findAccountResult = await _accountService.FindAccountByEmailAsync(email, clt);
            if(findAccountResult.IsSuccess)
            {
                Account foundAccount = findAccountResult.Value;
                
                bool hasNotVerifiedEmail = foundAccount.VerifiedAt == null;
                bool hasNotFinishedSignup = foundAccount.FinishedSignupAt == null;

                if(hasNotVerifiedEmail)
                {
                    return await HandleTokenAndSendEmailAsync(
                        foundAccount.AccountId, 
                        foundAccount.Email, 
                        TokenType.EmailVerification, 
                        EmailTemplate.EmailVerification, 
                        consistentCreatedAtDateTime, 
                        clt
                    );
                }

                if(hasNotFinishedSignup)
                {
                    // TODO: Niche edge case - may be possible that someone enters the email in signup while the original person requests recovery in the signin page
                    //      probably will just ignore it since it doesnt result in any invalid states, just is a little confusing for the real owner of the inbox if it occurs
                    return await HandleTokenAndSendEmailAsync(
                        foundAccount.AccountId, 
                        foundAccount.Email, 
                        TokenType.ResumeSignup, 
                        EmailTemplate.IncompleteAccountSignup, 
                        consistentCreatedAtDateTime, 
                        clt
                    );
                }

                _logger.LogWarning(
                    "Encountered signup attempt with email {Email} that already has a complete account. Sending warning email.",
                    foundAccount.Email
                );
                ServiceResult<Unit> emailSendResult = await _emailService.SendEmailAsync(foundAccount.Email, EmailTemplate.ExistingAccountSignup, clt);
                if(emailSendResult.IsSuccess)
                {
                    return ServiceResult<Unit>.Success(Unit.Value);
                }
                else
                {
                    return ServiceResult<Unit>.Failure(emailSendResult.ErrorCode!.Value);
                }
                
            }

            if(findAccountResult.ErrorCode != ServiceError.NoAccountFound)
            {
                return ServiceResult<Unit>.Failure(findAccountResult.ErrorCode!.Value);
            }

            ServiceResult<Account> createAccountResult = await _accountService.CreateAccountAsync(email, consistentCreatedAtDateTime, clt);
            if(createAccountResult.IsFailure) 
            {
                // isnt included as part of the following transaction rollback since its designed to utilize an account that exists but is not verified yet instead of
                //      deleting and creating another one. It can get cleaned up later anyways by db background job if the user who holds the email doesnt continue the flow
                _logger.LogError(
                    "Failed to create account for email {Email} - Error: {ErrorCode}",
                    email, 
                    createAccountResult.ErrorCode
                );
                return createAccountResult.ErrorCode switch
                {
                    ServiceError.InvalidInput => ServiceResult<Unit>.Failure(ServiceError.InvalidInput),
                    ServiceError.OperationCancelled => ServiceResult<Unit>.Failure(ServiceError.OperationCancelled),
                    _ => ServiceResult<Unit>.Failure(ServiceError.UnknownError)
                };
            }
            Account account = createAccountResult.Value;

            return await HandleTokenAndSendEmailAsync(
                account.AccountId, 
                account.Email, 
                TokenType.EmailVerification, 
                EmailTemplate.EmailVerification, 
                consistentCreatedAtDateTime, 
                clt
            );
        }

        // this method is a prime candidate for extraction into something more re-usable as other handlers may use it, just need to figure out where to put it
        //      I duplicated this and put it in SigninRequestRecoveryHandler.cs
        private async Task<ServiceResult<Unit>> HandleTokenAndSendEmailAsync(Guid accountId, string accountEmail, TokenType tokenType, EmailTemplate emailTemplate, DateTimeOffset consistentCreatedAtDateTime, CancellationToken clt)
        {
            await using var tx = await _unitOfWork.BeginTransactionAsync(clt);
            
            ServiceResult<Unit> invalidateTokensResult = await _tokenService.InvalidateTokenAsync(accountId, tokenType, clt);
            if(invalidateTokensResult.IsFailure)
            {
                await tx.RollbackAsync(CancellationToken.None);
                _logger.LogError(
                    "Failed to invalidate tokens with tokentype {TokenType} for email {Email} - Error: {ErrorCode}",
                    tokenType,
                    accountEmail,
                    invalidateTokensResult.ErrorCode
                );
                return ServiceResult<Unit>.Failure(invalidateTokensResult.ErrorCode!.Value); // TODO: Check if this is ok
            }


            ServiceResult<CreatedToken> createTokenResult = await _tokenService.CreateTokenAsync(
                accountId, 
                tokenType,
                consistentCreatedAtDateTime,
                clt
            );
            if(createTokenResult.IsFailure)
            {
                await tx.RollbackAsync(CancellationToken.None);
                _logger.LogError(
                    "Failed to create token with tokentype {TokenType} for email {Email} - Error: {ErrorCode}",
                    tokenType,
                    accountEmail,
                    createTokenResult.ErrorCode
                );
                return ServiceResult<Unit>.Failure(createTokenResult.ErrorCode!.Value); // TODO: Check if this is ok
            }
            CreatedToken token = createTokenResult.Value;


            // Later on, maybe use outbox pattern for retry logic so we dont have to rollback if the email fails to send. For now, just rollback
            ServiceResult<Unit> sendEmailResult = await _emailService.SendEmailAsync(accountEmail, emailTemplate, token.TokenRaw, clt);
            if(sendEmailResult.IsFailure)
            {
                await tx.RollbackAsync(CancellationToken.None);
                _logger.LogError(
                    "Failed to send email with tokentype {TokenType} for email {Email} - Error: {ErrorCode}",
                    tokenType,
                    accountEmail,
                    sendEmailResult.ErrorCode
                );
                return ServiceResult<Unit>.Failure(createTokenResult.ErrorCode!.Value); // TODO: Check if this is ok
            }

            await tx.CommitAsync(clt);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
    }
}