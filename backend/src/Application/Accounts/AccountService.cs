using backend.Application.Common.Interfaces;
using backend.Domain.Accounts;
using backend.Domain.Profiles;
using backend.Infrastructure.Common;
using backend.Infrastructure.Email;
using backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace backend.Application.Accounts
{
    public class AccountService(AppDbContext appDbContext, IEmailSender emailSender, IUsernameGenerator usernameGenerator)
    {
        private readonly AppDbContext _appDbContext = appDbContext;

        private readonly IEmailSender _emailSender = emailSender;

        private readonly IUsernameGenerator _usernameGenerator = usernameGenerator;

        private readonly PasswordHasher<Account> _passwordHasher = new();

        /*

        + CreateAccountAsync
        + GetUserByIdAsync
        + UpdateAccountAsync
        + DeleteAccountAsync
        */

        // public async Task<ServiceResult<bool>> CheckAccountExistsAsync(Guid accountId, CancellationToken clt)
        // {
            
        // }


        /*

        Remaining TODOs:
        - Fix account<->profile stuff (check appdbcontext)
        - Add verification email code generation
        - Add verification email sending

        Notes:
        - Didnt check for existing email since we allow duplicate unverified emails (but only one verified one)

        Followup questions:
        - If I ever change account/profile schema, I'll have to update this here. Is there a better way or is this the best we can do?

        */
        public async Task<ServiceResult<Account>> CreateAccountAsync(string email, string password, DateTime? createdAtOverride = null, CancellationToken clt = default)
        {
            if(string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return ServiceResult<Account>.Failure(ServiceError.InvalidInput);
            }
            DateTime createdAt = createdAtOverride ?? DateTime.UtcNow;
            
            await using var transaction = await _appDbContext.Database.BeginTransactionAsync(clt);
            try
            {
                Account newAccount = new()
                {
                    Email = email,
                    CreatedAt = createdAt,
                    HasVerifiedEmail = false
                };
                newAccount.PasswordHash = _passwordHasher.HashPassword(newAccount, password);
                
                string initialUsername = await _usernameGenerator.GenerateUsername();

                Profile newAccountProfile = new()
                {
                    AccountId = newAccount.AccountId,
                    Username = initialUsername,
                    CreatedAt = createdAt
                };

                _appDbContext.Accounts.Add(newAccount);
                _appDbContext.Profiles.Add(newAccountProfile);
                await _appDbContext.SaveChangesAsync(clt);

                await transaction.CommitAsync(clt);

                return ServiceResult<Account>.Success(newAccount);

            }
            catch (OperationCanceledException)
            {
                await transaction.RollbackAsync(clt);
                return ServiceResult<Account>.Failure(ServiceError.OperationCancelled);
            }
        }

        // public async Task<ServiceResult<bool>> TestAsync(CancellationToken clt)
        // {
        //     Console.WriteLine("Received test GET.");
        //     bool sent = await _emailSender.SendAsync(new EmailMessage("bob@mail.com", "TestEmail", "Hey there!"), clt);
        //     if(sent)
        //     {
        //         return ServiceResult<bool>.Success(true);
        //     }
        //     else
        //     {
        //         return ServiceResult<bool>.Failure(ServiceError.UnknownError);
        //     }
        // }
    }
}