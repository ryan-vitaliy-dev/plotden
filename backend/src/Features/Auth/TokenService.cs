using backend.Infrastructure.Persistence;

namespace backend.Features.Auth
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