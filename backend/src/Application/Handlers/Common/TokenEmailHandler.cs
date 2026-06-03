using Microsoft.Extensions.Logging;

using Application.Common;
using Application.Email;
using Application.Tokens;
using Application.Tokens.Results;
using Domain.Accounts;
using Domain.Common;
using Domain.Tokens;

namespace Application.Handlers.Common
{
    public class TokenEmailHandler(TokenService tokenService, EmailService emailService, ILogger<TokenEmailHandler> logger)
    {
        private readonly TokenService _tokenService = tokenService;
        private readonly EmailService _emailService = emailService;
        private readonly ILogger<TokenEmailHandler> _logger = logger;

        public async Task<ServiceResult<Unit>> GenerateTokenAndSendEmailAsync(Account account, TokenType tokenType, TokenEmailTemplate template, DateTimeOffset? createdAtOverride, CancellationToken clt)
        {
            // Invalidate old tokens
            // TODO: Move both invalidation and creation into a transaction (for rollback)
            ServiceResult<Unit> invalidateTokenResult = await _tokenService.InvalidateTokenAsync(account.AccountId, tokenType, clt);
            if(invalidateTokenResult.IsFailure)
            {
                // Do not issue a new token.
                return invalidateTokenResult.ErrorCode switch
                {
                    ServiceError.InvalidInput => ServiceResult<Unit>.Failure(ServiceError.InvalidInput),
                    ServiceError.OperationCancelled => ServiceResult<Unit>.Failure(ServiceError.OperationCancelled),
                    _ => ServiceResult<Unit>.Failure(ServiceError.UnknownError)
                };
            }

            ServiceResult<TokenCreationResult> tokenCreationResult = await _tokenService.CreateTokenAsync(account.AccountId, tokenType, createdAtOverride, clt);
            if(tokenCreationResult.IsFailure)
            {
                _logger.LogWarning("{TokenType} token creation failed for email {Email}. Error: {ErrorCode}", tokenType.ToString(), account.Email, tokenCreationResult.ErrorCode);
                return tokenCreationResult.ErrorCode switch
                {
                    ServiceError.InvalidInput => ServiceResult<Unit>.Failure(ServiceError.InvalidInput),
                    ServiceError.OperationCancelled => ServiceResult<Unit>.Failure(ServiceError.OperationCancelled),
                    _ => ServiceResult<Unit>.Failure(ServiceError.UnknownError)
                };
            }
            TokenCreationResult createdToken = tokenCreationResult.Value;

            ServiceResult<Unit> emailSendResult = tokenType switch
            {
                TokenType.EmailVerification => await _emailService.SendVerificationEmailAsync(account.Email, createdToken.TokenRaw, clt),
                TokenType.ResumeSignup => template == TokenEmailTemplate.ResumeSignup_Recovery
                    ? await _emailService.SendAccountExistsResumeSignupRecoveryEmailAsync(account.Email, createdToken.TokenRaw, clt)
                    : await _emailService.SendAccountExistsResumeSignupEmailAsync(account.Email, createdToken.TokenRaw, clt),
                TokenType.PasswordReset => await _emailService.SendPasswordResetEmailAsync(account.Email, createdToken.TokenRaw, clt),
                _ => ServiceResult<Unit>.Failure(ServiceError.UnknownError)
            };
            
            if(emailSendResult.IsFailure)
            {
                return ServiceResult<Unit>.Failure(emailSendResult.ErrorCode!.Value);
            }
            return ServiceResult<Unit>.Success(Unit.Value);
        }
    }
}