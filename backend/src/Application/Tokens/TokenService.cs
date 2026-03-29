using System.Security.Cryptography;
using System.Text;
using backend.Application.Common;
using backend.Application.Common.Interfaces;
using backend.Application.Tokens.DTOs;
using backend.Domain.Tokens;
using backend.Infrastructure.Common;
using backend.Infrastructure.Persistence;
using backend.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace backend.Application.Tokens
{
    public class TokenService(AppDbContext appDbContext, ITokenGenerator tokenGenerator)
    {
        private readonly AppDbContext _appDbContext = appDbContext;

        private readonly ITokenGenerator _tokenGenerator = tokenGenerator;

        /*

            + CreateTokenAsync
            + FindTokenByHash
        + VerifyTokenAsync
        + InvalidateTokenAsync
        
        */

        public async Task<ServiceResult<TokenCreationResult>> CreateTokenAsync(Guid accountId, TokenType tokenType, DateTimeOffset? createdAtOverride = null, CancellationToken clt = default)
        {
            if(accountId == Guid.Empty)
            {
                return ServiceResult<TokenCreationResult>.Failure(ServiceError.InvalidInput);
            }
            try
            {
                DateTimeOffset createdAt = createdAtOverride ?? DateTimeOffset.UtcNow;
                DateTimeOffset expiresAt = createdAt.AddMinutes(15); // TODO: make token expiration configurable, maybe depends on token type
                string generatedToken = _tokenGenerator.GenerateToken(); 

                byte[] generatedTokenBytes = Encoding.UTF8.GetBytes(generatedToken);
                byte[] tokenHashBytes = SHA256.HashData(generatedTokenBytes);
                string tokenHash = Convert.ToHexStringLower(tokenHashBytes);
                
                Token newToken = new()
                {
                    TokenHash = tokenHash,
                    AccountId = accountId,
                    TokenType = tokenType,
                    CreatedAt = createdAt,
                    ExpiresAt = expiresAt
                };

                await _appDbContext.Tokens.AddAsync(newToken, clt);
                await _appDbContext.SaveChangesAsync(clt);
                TokenCreationResult result = new(generatedToken);
                return ServiceResult<TokenCreationResult>.Success(result);    
            }
            catch (OperationCanceledException)
            {
                return ServiceResult<TokenCreationResult>.Failure(ServiceError.OperationCancelled);
            }
        }

        public async Task<ServiceResult<Token>> FindTokenByHashAsync(string tokenHash, CancellationToken clt)
        {
            if(string.IsNullOrWhiteSpace(tokenHash))
            {
                return ServiceResult<Token>.Failure(ServiceError.InvalidInput);
            }
            try
            {
                Token? foundToken = await _appDbContext.Tokens
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, clt);
                if(foundToken == null)
                {
                    return ServiceResult<Token>.Failure(ServiceError.NoTokenFound);
                }
                return ServiceResult<Token>.Success(foundToken);
            }
            catch (OperationCanceledException)
            {
                return ServiceResult<Token>.Failure(ServiceError.OperationCancelled);
            }
        }

        public async Task<ServiceResult<Unit>> InvalidateTokenAsync(Guid accountId, TokenType tokenType, CancellationToken clt)
        {
            if(accountId == Guid.Empty)
            {
                return ServiceResult<Unit>.Failure(ServiceError.InvalidInput);
            }
            try
            {
                Token? foundToken = await _appDbContext.Tokens
                    .FirstOrDefaultAsync(t => 
                        t.AccountId == accountId && 
                        t.TokenType == tokenType, 
                    clt);
                if(foundToken == null)
                {
                    return ServiceResult<Unit>.Success(Unit.Value);
                }
                foundToken.RevokedAt = DateTimeOffset.UtcNow;
                await _appDbContext.SaveChangesAsync(clt);
                return ServiceResult<Unit>.Success(Unit.Value);
            }
            catch (OperationCanceledException)
            {
                return ServiceResult<Unit>.Failure(ServiceError.OperationCancelled);
            }
        }

        // Not currently in use. May need later, so keeping it for now.
        // public async Task<ServiceResult<Token>> FindActiveTokenByAccountAndTypeAsync(Guid accountId, TokenType tokenType, CancellationToken clt)
        // {
        //     if(accountId == Guid.Empty)
        //     {
        //         return ServiceResult<Token>.Failure(ServiceError.InvalidInput);
        //     }
        //     try
        //     {
        //         Token? foundToken = await _appDbContext.Tokens
        //             .AsNoTracking()
        //             .FirstOrDefaultAsync(t => 
        //                 t.AccountId == accountId && 
        //                 t.TokenType == tokenType &&
        //                 t.RevokedAt == null &&
        //                 t.ConsumedAt == null &&
        //                 t.ExpiresAt > DateTimeOffset.UtcNow, 
        //             clt);
        //         if(foundToken == null)
        //         {
        //             return ServiceResult<Token>.Failure(ServiceError.NoTokenFound);
        //         }
        //         return ServiceResult<Token>.Success(foundToken);
        //     }
        //     catch (OperationCanceledException)
        //     {
        //         return ServiceResult<Token>.Failure(ServiceError.OperationCancelled);
        //     }
        // }
    }
}