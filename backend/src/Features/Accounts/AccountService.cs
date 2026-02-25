using backend.Infrastructure.Common;
using backend.Infrastructure.Email;
using backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace backend.Features.Accounts
{
    public class AccountService(AppDbContext context, IEmailSender emailSender)
    {
        private readonly AppDbContext _context = context;

        private readonly IEmailSender _emailSender = emailSender;

        private readonly PasswordHasher<Account> _passwordHasher = new();

        /*

        + CreateUserAsync
        + GetUserByIdAsync
        + UpdateUserAsync
        + DeleteUserAsync
        */

        public async Task<ServiceResult<bool>> TestAsync()
        {
            bool sent = await _emailSender.SendAsync(new EmailMessage("bob@mail.com", "TestEmail", "Hey there!"));
            Console.WriteLine("test");
            if(sent)
            {
                return ServiceResult<bool>.Success(true);
            }
            else
            {
                return ServiceResult<bool>.Failure(ServiceError.UnknownError);
            }
        }
    }
}