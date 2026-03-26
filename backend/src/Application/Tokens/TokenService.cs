using System.Security.Cryptography;
using System.Text;
using backend.Application.Common.Interfaces;
using backend.Domain.Tokens;
using backend.Infrastructure.Common;
using backend.Infrastructure.Persistence;
using backend.Infrastructure.Security;

namespace backend.Application.Tokens
{
    public class TokenService(AppDbContext context, ITokenGenerator tokenGenerator)
    {
        private readonly AppDbContext _context = context;

        private readonly ITokenGenerator _tokenGenerator = tokenGenerator;

        /*

            + CreateTokenAsync
        + GetTokenAsync
        + VerifyTokenAsync
        + InvalidateTokenAsync
        
        */

        public async Task<ServiceResult<Token>> CreateTokenAsync(Guid accountId, TokenType tokenType, DateTimeOffset? createdAtOverride = null, CancellationToken clt = default)
        {
            if(accountId == Guid.Empty)
            {
                return ServiceResult<Token>.Failure(ServiceError.InvalidInput);
            }
            try
            {
                DateTimeOffset createdAt = createdAtOverride ?? DateTimeOffset.UtcNow;
                DateTimeOffset expiresAt = createdAt.AddMinutes(15); // TODO: make token expiration configurable, maybe depends on token type
                string generatedToken = _tokenGenerator.GenerateToken(); 

                byte[] generatedTokenBytes = Encoding.UTF8.GetBytes(generatedToken);
                byte[] tokenHashBytes = SHA256.HashData(generatedTokenBytes);
                string tokenHash = Convert.ToHexString(tokenHashBytes).ToLowerInvariant();
                
                Token newToken = new()
                {
                    TokenHash = tokenHash,
                    AccountId = accountId,
                    TokenType = tokenType,
                    CreatedAt = createdAt,
                    ExpiresAt = expiresAt
                };

                await _context.Tokens.AddAsync(newToken, clt);
                await _context.SaveChangesAsync(clt);
                return ServiceResult<Token>.Success(newToken);    
            }
            catch (OperationCanceledException)
            {
                return ServiceResult<Token>.Failure(ServiceError.OperationCancelled);
            }
        }
    }
}