using backend.Infrastructure.Persistence;

namespace backend.Application.Tokens
{
    public class TokenService(AppDbContext context)
    {
        private readonly AppDbContext _context = context;

        /*

        + CreateTokenAsync
        + GetTokenAsync
        + VerifyTokenAsync
        + InvalidateTokenAsync
        
        */
    }
}