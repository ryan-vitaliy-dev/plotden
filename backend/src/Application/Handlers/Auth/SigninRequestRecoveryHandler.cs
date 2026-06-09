using Microsoft.Extensions.Logging;

using Application.Accounts;
using Application.Common;
using Application.Sessions;
using Domain.Accounts;
using Domain.Common;
using Domain.Tokens;
using Application.Common.Interfaces;
using Application.Tokens;
using Application.Tokens.Results;
using Application.Email;

namespace Application.Handlers.Auth
{
    public class SigninRequestRecoveryHandler(
        ILogger<SigninRequestRecoveryHandler> logger,
        IUnitOfWork unitOfWork,
        AccountService accountService, 
        SessionService sessionService, 
        TokenService tokenService,
        EmailService emailService
    )
    {
        private readonly ILogger<SigninRequestRecoveryHandler> _logger = logger;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        
        private readonly AccountService _accountService = accountService;
        private readonly SessionService _sessionService = sessionService;
        private readonly TokenService _tokenService = tokenService;
        private readonly EmailService _emailService = emailService;



        public async Task<ServiceResult<Unit>> HandleAsync(string email, CancellationToken clt)
        {
            ServiceResult<Account> findAccountResult = await _accountService.FindAccountByEmailAsync(email, clt);
            if(findAccountResult.IsFailure)
            {
                // If no account found, return success anyways. Still technically vulnerable to timing attacks but we can rate limit
                return ServiceResult<Unit>.Failure(ServiceError.NoAccountFound);
            }
            Account account = findAccountResult.Value;

            bool hasFinishedSignup = account.FinishedSignupAt != null;
            bool hasVerifiedEmail = account.VerifiedAt != null;

            if(hasFinishedSignup)
            {
                ServiceResult<Unit> generatePasswordResetEmailResult = await HandleTokenAndSendEmailAsync(
                    account.AccountId, 
                    account.Email, 
                    TokenType.PasswordReset, 
                    EmailTemplate.PasswordReset,
                    DateTimeOffset.UtcNow, 
                    clt
                );
                if(generatePasswordResetEmailResult.IsFailure)
                {
                    return ServiceResult<Unit>.Failure(generatePasswordResetEmailResult.ErrorCode!.Value); // TODO: See if this is OK.
                }
                return ServiceResult<Unit>.Success(Unit.Value);
            }

            if(hasVerifiedEmail)
            {
                ServiceResult<Unit> generateResumeSignupEmailResult = await HandleTokenAndSendEmailAsync(
                    account.AccountId,
                    account.Email,
                    TokenType.ResumeSignup,
                    EmailTemplate.IncompleteAccountRecovery,
                    DateTimeOffset.UtcNow,
                    clt
                );
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


        // this method is a prime candidate for extraction into something more re-usable as other handlers may use it, just need to figure out where to put it
        //      this method is also in SignupRequestEmailHandler.cs
        private async Task<ServiceResult<Unit>> HandleTokenAndSendEmailAsync(Guid accountId, string accountEmail, TokenType tokenType, EmailTemplate emailTemplate, DateTimeOffset consistentCreatedAtDateTime, CancellationToken clt)
        {
            await using var tx = await _unitOfWork.BeginTransactionAsync(clt);
            
            ServiceResult<Unit> invalidateTokensResult = await _tokenService.InvalidateTokenAsync(accountId, tokenType, clt);
            if(invalidateTokensResult.IsFailure)
            {
                await tx.RollbackAsync(CancellationToken.None);
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
                return ServiceResult<Unit>.Failure(createTokenResult.ErrorCode!.Value); // TODO: Check if this is ok
            }
            CreatedToken token = createTokenResult.Value;


            // Later on, maybe use outbox pattern for retry logic so we dont have to rollback if the email fails to send. For now, just rollback
            ServiceResult<Unit> sendEmailResult = await _emailService.SendEmailAsync(
                accountEmail, 
                emailTemplate, 
                new Dictionary<string, string> { 
                    ["RawToken"] = token.TokenRaw 
                }, 
                clt
            );
            if(sendEmailResult.IsFailure)
            {
                await tx.RollbackAsync(CancellationToken.None);
                return ServiceResult<Unit>.Failure(createTokenResult.ErrorCode!.Value); // TODO: Check if this is ok
            }

            await tx.CommitAsync(clt);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
    }
}