using Application.Auth.Results;
using Application.Common;
using Application.Email;
using Application.Tokens;
using Application.Tokens.Results;
using Domain.Accounts;
using Domain.Common;
using Domain.Tokens;
using Microsoft.Extensions.Logging;

namespace Application.Handlers.Common
{
    public class TokenEmailHandler(TokenService tokenService, EmailService emailService, ILogger<TokenEmailHandler> logger)
    {
        private readonly TokenService _tokenService = tokenService;
        private readonly EmailService _emailService = emailService;
        private readonly ILogger<TokenEmailHandler> _logger = logger;

        public async Task<ServiceResult<SignupEmailResult>> GenerateTokenAndSendEmailAsync(Account account, TokenType tokenType, DateTimeOffset? createdAtOverride, CancellationToken clt)
        {
            // Invalidate old tokens
            // TODO: Move both invalidation and creation into a transaction (for rollback)
            ServiceResult<Unit> invalidateTokenResult = await _tokenService.InvalidateTokenAsync(account.AccountId, tokenType, clt);
            if(invalidateTokenResult.IsFailure)
            {
                // Do not issue a new token.
                return invalidateTokenResult.ErrorCode switch
                {
                    ServiceError.InvalidInput => ServiceResult<SignupEmailResult>.Failure(ServiceError.InvalidInput),
                    ServiceError.OperationCancelled => ServiceResult<SignupEmailResult>.Failure(ServiceError.OperationCancelled),
                    _ => ServiceResult<SignupEmailResult>.Failure(ServiceError.UnknownError)
                };
            }

            ServiceResult<TokenCreationResult> tokenCreationResult = await _tokenService.CreateTokenAsync(account.AccountId, tokenType, createdAtOverride, clt);
            if(tokenCreationResult.IsFailure)
            {
                _logger.LogWarning("{TokenType} token creation failed for email {Email}. Error: {ErrorCode}", tokenType.ToString(), account.Email, tokenCreationResult.ErrorCode);
                return tokenCreationResult.ErrorCode switch
                {
                    ServiceError.InvalidInput => ServiceResult<SignupEmailResult>.Failure(ServiceError.InvalidInput),
                    ServiceError.OperationCancelled => ServiceResult<SignupEmailResult>.Failure(ServiceError.OperationCancelled),
                    _ => ServiceResult<SignupEmailResult>.Failure(ServiceError.UnknownError)
                };
            }
            TokenCreationResult createdToken = tokenCreationResult.Value;

            ServiceResult<Unit> emailSendResult = tokenType switch
            {
                TokenType.EmailVerification => await _emailService.SendVerificationEmailAsync(account.Email, createdToken.TokenRaw, clt),
                TokenType.ResumeSignup => await _emailService.SendAccountExistsResumeSignupEmailAsync(account.Email, createdToken.TokenRaw, clt),
                // WIP
                _ => ServiceResult<Unit>.Failure(ServiceError.UnknownError)
            };
            
            if(emailSendResult.IsFailure)
            {
                return ServiceResult<SignupEmailResult>.Failure(emailSendResult.ErrorCode!.Value);
            }

            SignupEmailResult result = new(account.Email);
            return ServiceResult<SignupEmailResult>.Success(result);
        }
    }
}