using Microsoft.Extensions.Logging;

using System.Text;
using System.Security.Cryptography;

using Application.Accounts;
using Application.Common;
using Application.Sessions;
using Application.Tokens;
using Application.Email;
using Application.Common.Interfaces;
using Domain.Accounts;
using Domain.Common;
using Domain.Tokens;

namespace Application.Handlers.Accounts
{
    public class ConfirmEmailUpdateHandler(
        ILogger<ConfirmEmailUpdateHandler> logger,
        IUnitOfWork unitOfWork,
        EmailUpdateRequestService emailUpdateRequestService,
        AccountService accountService, 
        TokenService tokenService,
        SessionService sessionService,
        EmailService emailService
    )
    {
        private readonly ILogger<ConfirmEmailUpdateHandler> _logger = logger;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;


        private readonly EmailUpdateRequestService _emailUpdateRequestService = emailUpdateRequestService;
        private readonly AccountService _accountService = accountService;
        private readonly TokenService _tokenService = tokenService;
        private readonly SessionService _sessionService = sessionService;

        private readonly EmailService _emailService = emailService;

        public async Task<ServiceResult<Unit>> HandleAsync(string rawToken, CancellationToken clt)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
            {
                return ServiceResult<Unit>.Failure(ServiceError.InvalidInput);
            }

            byte[] generatedTokenBytes = Encoding.UTF8.GetBytes(rawToken);
            byte[] tokenHashBytes = SHA256.HashData(generatedTokenBytes);
            string tokenHash = Convert.ToHexStringLower(tokenHashBytes);

            ServiceResult<Token> findTokenResult = await _tokenService.FindActiveTokenByHashAsync(tokenHash, clt);
            if(findTokenResult.IsFailure)
            {
                _logger.LogError("Failed to find token with hash {TokenHash}", tokenHash);
                return ServiceResult<Unit>.Failure(ServiceError.NoTokenFound);
            }
            Token token = findTokenResult.Value;
            
            ServiceResult<Account> findAccountResult = await _accountService.FindAccountByIdAsync(token.AccountId, clt);
            if(findAccountResult.IsFailure)
            {
                _logger.LogError("Failed to find account with id {AccountId} for token hash {TokenHash}", token.AccountId, token.TokenHash);
                return ServiceResult<Unit>.Failure(ServiceError.NoAccountFound);
            }
            Account account = findAccountResult.Value;

            // TODO: Make it by request id instead of account id, since if something fails and there are multiple requests, we might not be able to identify the correct one
            ServiceResult<EmailUpdateRequest> findEmailUpdateRequestResult = await _emailUpdateRequestService.FindEmailUpdateRequestByAccountIdAsync(account.AccountId, clt);
            if(findEmailUpdateRequestResult.IsFailure)
            {
                _logger.LogError(
                    "Failed to find email update request for account with id {AccountId} - Error: {ErrorCode}",
                    account.AccountId,
                    findEmailUpdateRequestResult.ErrorCode
                );
                return ServiceResult<Unit>.Failure(findEmailUpdateRequestResult.ErrorCode!.Value);
            }
            EmailUpdateRequest emailUpdateRequest = findEmailUpdateRequestResult.Value;


            await using var tx = await _unitOfWork.BeginTransactionAsync(clt);

            ServiceResult<Unit> consumeTokenResult = await _tokenService.ConsumeTokenAsync(token, clt);
            if(consumeTokenResult.IsFailure)
            {
                await tx.RollbackAsync(CancellationToken.None);
                _logger.LogError(
                    "Failed to consume token with token hash {TokenHash} for account with id {AccountId} - Error: {ErrorCode}", 
                    token.TokenHash,
                    account.AccountId,
                    consumeTokenResult.ErrorCode
                );
                return ServiceResult<Unit>.Failure(consumeTokenResult.ErrorCode!.Value); // TODO: Check if this is ok
            }

            ServiceResult<Unit> updateEmailResult = await _accountService.SetEmailAsync(account, emailUpdateRequest.NewEmail, clt);
            if(updateEmailResult.IsFailure)
            {
                await tx.RollbackAsync(CancellationToken.None);
                _logger.LogError(
                    "Failed to update email for account with id {AccountId} - Error: {ErrorCode}",
                    account.AccountId,
                    updateEmailResult.ErrorCode
                );
                return ServiceResult<Unit>.Failure(updateEmailResult.ErrorCode!.Value);
            }

            ServiceResult<Unit> consumeEmailUpdateRequestResult = await _emailUpdateRequestService.DeleteEmailUpdateRequestAsync(account.AccountId, clt);
            if(consumeEmailUpdateRequestResult.IsFailure)
            {
                await tx.RollbackAsync(CancellationToken.None);
                _logger.LogError(
                    "Failed to remove email update request for account with id {AccountId} - Error: {ErrorCode}",
                    account.AccountId,
                    findEmailUpdateRequestResult.ErrorCode
                );
                return ServiceResult<Unit>.Failure(consumeEmailUpdateRequestResult.ErrorCode!.Value);
            }

            // Later on, maybe use outbox pattern for retry logic so we dont have to rollback if the email fails to send. For now, just rollback
            ServiceResult<Unit> sendEmailResult = await _emailService.SendEmailAsync(
                emailUpdateRequest.OldEmail, 
                EmailTemplate.EmailUpdateNotice, 
                new Dictionary<string, string> {
                    ["NewEmail"] = emailUpdateRequest.NewEmail
                }, 
                clt
            );
            if(sendEmailResult.IsFailure)
            {
                await tx.RollbackAsync(CancellationToken.None);
                _logger.LogWarning(
                    "Failed to send email update notice for account with id {AccountId} - Error: {ErrorCode}",
                    account.AccountId,
                    sendEmailResult.ErrorCode
                );
                return ServiceResult<Unit>.Failure(sendEmailResult.ErrorCode!.Value); // TODO: Check if this is ok
            }

            // TODO: Check if I should invalidate all sessions and make them sign in again, probably should but for now leave it be

            await tx.CommitAsync(clt);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
    }
}