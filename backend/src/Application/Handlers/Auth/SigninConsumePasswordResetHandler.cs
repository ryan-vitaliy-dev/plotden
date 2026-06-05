using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

using Application.Accounts;
using Application.Resources;
using Application.Common;
using Application.Sessions;
using Application.Tokens;
using Domain.Accounts;
using Domain.Sessions;
using Domain.Tokens;
using Domain.Common;
using Application.Common.Interfaces;
using Application.Sessions.Results;

namespace Application.Handlers.Auth
{
    public class SigninConsumePasswordResetHandler(
        ILogger<SigninConsumePasswordResetHandler> logger,
        IStringLocalizer<SharedResource> localizer, 
        IUnitOfWork unitOfWork,
        AccountService accountService,
        TokenService tokenService,
        SessionService sessionService
    )
    {
        private readonly ILogger<SigninConsumePasswordResetHandler> _logger = logger;
        private readonly IStringLocalizer<SharedResource> _localizer = localizer;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        
        private readonly TokenService _tokenService = tokenService;
        private readonly AccountService _accountService = accountService;
        private readonly SessionService _sessionService = sessionService;

        public async Task<ServiceResult<CreatedSession>> HandleAsync(string rawToken, ClientInfo clientInfo, CancellationToken clt)
        {
            byte[] generatedTokenBytes = Encoding.UTF8.GetBytes(rawToken);
            byte[] tokenHashBytes = SHA256.HashData(generatedTokenBytes);
            string tokenHash = Convert.ToHexStringLower(tokenHashBytes);

            ServiceResult<Token> findTokenResult = await _tokenService.FindActiveTokenByHashAsync(tokenHash, clt);
            if(findTokenResult.IsFailure)
            {
                return ServiceResult<CreatedSession>.Failure(findTokenResult.ErrorCode!.Value);
            }
            Token token = findTokenResult.Value;

            ServiceResult<Account> findAccountResult = await _accountService.FindAccountByIdAsync(token.AccountId, clt);
            if(findAccountResult.IsFailure)
            {
                _logger.LogError("Failed to find account with id {AccountId} for token hash {TokenHash}", token.AccountId, token.TokenHash);
                return ServiceResult<CreatedSession>.Failure(findAccountResult.ErrorCode!.Value);
            }
            Account account = findAccountResult.Value;


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
                return ServiceResult<CreatedSession>.Failure(consumeTokenResult.ErrorCode!.Value); // TODO: Check if this is ok
            }


            ServiceResult<Unit> invalidateSessionsResult = await _sessionService.InvalidateSessionsAsync(account.AccountId, clt);
            if(invalidateSessionsResult.IsFailure)
            {
                await tx.RollbackAsync(CancellationToken.None);
                _logger.LogError(
                    "Failed to invalidate sessions for account with id {AccountId} - Error: {ErrorCode}", 
                    account.AccountId,
                    invalidateSessionsResult.ErrorCode
                );
                return ServiceResult<CreatedSession>.Failure(invalidateSessionsResult.ErrorCode!.Value); // TODO: Check if this is ok
            }


            ServiceResult<Session> createSessionResult = await _sessionService.CreateSessionAsync(
                account.AccountId, 
                SessionType.PasswordReset, 
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

            await tx.CommitAsync(clt);


            Session createdSession = createSessionResult.Value;

            CreatedSession result = new(createdSession.SessionId.ToString(), createdSession.ExpiresAt);

            return ServiceResult<CreatedSession>.Success(result);
        }
    }
}