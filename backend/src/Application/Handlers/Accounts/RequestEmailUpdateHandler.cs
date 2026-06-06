using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using Application.Accounts;
using Application.Common;
using Application.Tokens;
using Domain.Accounts;
using Domain.Tokens;
using Domain.Common;
using Application.Common.Interfaces;
using Application.Tokens.Results;
using Application.Email;

namespace Application.Handlers.Accounts
{
    public class RequestEmailUpdateHandler(
        ILogger<RequestEmailUpdateHandler> logger,
        IUnitOfWork unitOfWork,
        AccountService accountService, 
        TokenService tokenService,
        EmailUpdateRequestService emailUpdateRequestService,
        EmailService emailService
    )
    {
        private readonly ILogger<RequestEmailUpdateHandler> _logger = logger;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        private readonly AccountService _accountService = accountService;
        private readonly TokenService _tokenService = tokenService;

        private readonly EmailUpdateRequestService _emailUpdateRequestService = emailUpdateRequestService;
        private readonly EmailService _emailService = emailService;
        
        private readonly PasswordHasher<Account> _passwordHasher = new();

        public async Task<ServiceResult<Unit>> HandleAsync(Guid accountId, string newEmail, string password, CancellationToken clt)
        {
            if (string.IsNullOrWhiteSpace(newEmail) || string.IsNullOrWhiteSpace(password))
            {
                return ServiceResult<Unit>.Failure(ServiceError.InvalidInput);
            }
            
            ServiceResult<Account> findAccountResult = await _accountService.FindAccountByIdAsync(accountId, clt);
            if(findAccountResult.IsFailure)
            {
                _logger.LogError(
                    "Failed to find account with id {AccountId} - Error: {ErrorCode}",
                    accountId,
                    findAccountResult.ErrorCode
                );
                return ServiceResult<Unit>.Failure(ServiceError.NoAccountFound);
            }
            Account account = findAccountResult.Value;

            if(account.PasswordHash == null)
            {
                _logger.LogWarning(
                    "Account with id {AccountId} attempted to update their email, but has not finished signup",
                    accountId
                );
                return ServiceResult<Unit>.Failure(ServiceError.PasswordNotSet);
            }

            PasswordVerificationResult passwordVerificationResult = _passwordHasher.VerifyHashedPassword(account, account.PasswordHash, password);
            if(passwordVerificationResult == PasswordVerificationResult.Failed)
            {
                return ServiceResult<Unit>.Failure(ServiceError.InvalidCredentials);
            }
            // TODO: handle rehash stuff later if needed


            await using var tx = await _unitOfWork.BeginTransactionAsync(clt);

            // Invalidate previous tokens of the same type, create new token, send it to the new email address, return success either way

            ServiceResult<Unit> invalidateTokensResult = await _tokenService.InvalidateTokenAsync(accountId, TokenType.EmailUpdate, clt);
            if(invalidateTokensResult.IsFailure)
            {
                await tx.RollbackAsync(CancellationToken.None);
                _logger.LogError(
                    "Failed to invalidate tokens with tokentype {TokenType} for account {AccountId} - Error: {ErrorCode}",
                    TokenType.EmailUpdate,
                    accountId,
                    invalidateTokensResult.ErrorCode
                );
                return ServiceResult<Unit>.Failure(invalidateTokensResult.ErrorCode!.Value); // TODO: Check if this is ok
            }


            ServiceResult<CreatedToken> createTokenResult = await _tokenService.CreateTokenAsync(
                accountId, 
                TokenType.EmailUpdate,
                null,
                clt
            );
            if(createTokenResult.IsFailure)
            {
                await tx.RollbackAsync(CancellationToken.None);
                _logger.LogError(
                    "Failed to create token with tokentype {TokenType} for account {AccountId} - Error: {ErrorCode}",
                    TokenType.EmailUpdate,
                    accountId,
                    createTokenResult.ErrorCode
                );
                return ServiceResult<Unit>.Failure(createTokenResult.ErrorCode!.Value); // TODO: Check if this is ok
            }
            CreatedToken token = createTokenResult.Value;

            // TODO: Check to see if email is in use first - note that we should reject it silently but show that it succeeded to prevent email enumeration

            ServiceResult<Unit> createEmailUpdateRequestResult = await _emailUpdateRequestService.CreateEmailUpdateRequestAsync(
                accountId, 
                account.Email, 
                newEmail, 
                null,
                clt
            );
            if(createEmailUpdateRequestResult.IsFailure)
            {
                await tx.RollbackAsync(CancellationToken.None);
                _logger.LogError(
                    "Failed to create email update request for account {AccountId} - Error: {ErrorCode}",
                    accountId,
                    createEmailUpdateRequestResult.ErrorCode
                );
                return ServiceResult<Unit>.Failure(createEmailUpdateRequestResult.ErrorCode!.Value); // TODO: Check if this is ok
            }

            // Later on, maybe use outbox pattern for retry logic so we dont have to rollback if the email fails to send. For now, just rollback
            ServiceResult<Unit> sendEmailResult = await _emailService.SendEmailAsync(newEmail, EmailTemplate.EmailUpdate, token.TokenRaw, clt);
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