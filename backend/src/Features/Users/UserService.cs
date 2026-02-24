using backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace backend.Features.Users
{
    public class UserService(AppDbContext context)
    {
        private readonly AppDbContext _context = context;

        private readonly PasswordHasher<User> _passwordHasher = new();

        /*

        + CreateUserAsync
        + GetUserByIdAsync
        + UpdateUserAsync
        + DeleteUserAsync
        */
    }
}