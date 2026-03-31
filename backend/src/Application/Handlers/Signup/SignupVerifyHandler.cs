

using System.Net;
using System.Security.Cryptography;
using System.Text;
using backend.API.DTOs.Accounts;
using backend.Application.Accounts;
using backend.Application.Accounts.DTOs;
using backend.Application.Common;
using backend.Application.Sessions;
using backend.Application.Tokens;
using backend.Domain.Accounts;
using backend.Domain.Sessions;
using backend.Domain.Tokens;
using backend.Infrastructure.Common;
using backend.Resources;
using Microsoft.Extensions.Localization;

namespace backend.Application.Handlers.Signup
{
    public class SignupVerifyHandler(
        ILogger<SignupVerifyHandler> logger, 
        AccountService accountService, 
        TokenService tokenService,
        SessionService sessionService, 
        // IEmailSender emailSender,
        IStringLocalizer<SharedResource> localizer)
    {
        private readonly ILogger<SignupVerifyHandler> _logger = logger;

        private readonly TokenService _tokenService = tokenService;

        private readonly AccountService _accountService = accountService;
        private readonly SessionService _sessionService = sessionService;
        private readonly IStringLocalizer<SharedResource> _localizer = localizer;

        public async Task<ServiceResult<AccountSignupVerifyResult>> HandleAsync(AccountSignupVerifyDTO dto, IPAddress? ipAddress, string? userAgent, CancellationToken clt)
        {
            // 1. Hash token in query
            // 2. Check token
            // 3. If not exists, return error that its expired/invalid
            // 4. If exists, check if its expired/still active
            // 5. If not, return error that its expired/invalid
            // 6. If yes, consume it and mark account as verified
            // 7. Cont: Attach a session cookie
            // 8. Cont: Return 200 OK
            
            byte[] generatedTokenBytes = Encoding.UTF8.GetBytes(dto.Token);
            byte[] tokenHashBytes = SHA256.HashData(generatedTokenBytes);
            string tokenHash = Convert.ToHexStringLower(tokenHashBytes);

            ServiceResult<Token> findMatchingTokenResult = await _tokenService.FindActiveTokenByHashAsync(tokenHash, clt);
            if(findMatchingTokenResult.IsFailure)
            {
                // Either a DB error such as a connection error or the token is invalid/expired
                return ServiceResult<AccountSignupVerifyResult>.Failure(findMatchingTokenResult.ErrorCode!.Value);
            }
            Token matchingToken = findMatchingTokenResult.Value;

            ServiceResult<Account> findMatchingAccountResult = await _accountService.FindAccountByIdAsync(matchingToken.AccountId, clt);
            if(findMatchingAccountResult.IsFailure)
            {
                // Either a DB error such as connection error or some other unexpected error, log it for safety
                _logger.LogError("Could not find matching account with id {AccountId} for token hash {TokenHash}", matchingToken.AccountId, matchingToken.TokenHash);
                return ServiceResult<AccountSignupVerifyResult>.Failure(findMatchingAccountResult.ErrorCode!.Value);
            }
            Account matchingAccount = findMatchingAccountResult.Value;

            // TODO: Make consuming the token and marking the account as verified an atomic operation (via transaction)
            // note- may need to make a VerificationService.cs or some other way to tie the two together since we dont want db-level stuff in handlers

            ServiceResult<Unit> consumeTokenResult = await _tokenService.ConsumeTokenAsync(matchingToken, clt);
            if(consumeTokenResult.IsFailure)
            {
                // TODO: what should we do if it fails to mark it as consumed?
                return ServiceResult<AccountSignupVerifyResult>.Failure(consumeTokenResult.ErrorCode!.Value);
            }

            ServiceResult<Unit> verifyAccountEmailResult = await _accountService.VerifyAccountEmailAsync(matchingAccount, clt);
            if(verifyAccountEmailResult.IsFailure)
            {
                // TODO: what should we do if it fails to mark it as verified?
                return ServiceResult<AccountSignupVerifyResult>.Failure(consumeTokenResult.ErrorCode!.Value);
            }

            // Create a session
            ServiceResult<Session> sessionCreationResult = await _sessionService.CreateSessionAsync(matchingAccount.AccountId, ipAddress, userAgent, null, clt);
            if(sessionCreationResult.IsFailure)
            {
                // TODO: what should we do if it fails to make a session?
                return ServiceResult<AccountSignupVerifyResult>.Failure(consumeTokenResult.ErrorCode!.Value);
            }
            Session createdSession = sessionCreationResult.Value;

            AccountSignupVerifyResult result = new(createdSession.SessionId.ToString(), createdSession.ExpiresAt);
            return ServiceResult<AccountSignupVerifyResult>.Success(result);
        }
    }
}