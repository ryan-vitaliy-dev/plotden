using backend.Infrastructure.Persistence;

namespace backend.Application.Sessions
{
    public class SessionService(AppDbContext context)
    {
        private readonly AppDbContext _context = context;

        /*

        + CreateSessionAsync
        + GetSessionByIdAsync
        + InvalidateSessionAsync

        */
    }
}