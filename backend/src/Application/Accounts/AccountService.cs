using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Application.Common;
using Application.Common.Interfaces;
using Domain.Accounts;
using Domain.Common;
using Domain.Profiles;

namespace Application.Accounts
{
    public class AccountService(IAppDbContext appDbContext, IEmailSender emailSender, ILogger<AccountService> logger, IUsernameGenerator usernameGenerator)
    {
        private readonly IAppDbContext _appDbContext = appDbContext;

        private readonly IEmailSender _emailSender = emailSender;

        private readonly ILogger<AccountService> _logger = logger;

        private readonly IUsernameGenerator _usernameGenerator = usernameGenerator;

        private readonly PasswordHasher<Account> _passwordHasher = new();

        /*

            + CreateAccountAsync
            + FindAccountByIdAsync
            + FindAccountByEmailAsync
        + UpdateAccountAsync
        + DeleteAccountAsync
        */


        /// <summary>
        /// Creates a new <see cref="Account"/>.
        /// </summary>
        /// <param name="email">The email for the new account.</param>
        /// <param name="createdAtOverride">Optional, the <see cref="DateTimeOffset"/> to use instead of the default (<see cref="DateTimeOffset.UtcNow"/>).</param>
        /// <param name="clt">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
        /// <returns>
        /// A <see cref="ServiceResult{T}"/> containing an <see cref="Account"/> if the creation succeeds,
        /// or a failure with an appropriate <see cref="ServiceError"/>.
        /// </returns>
        public async Task<ServiceResult<Account>> CreateAccountAsync(string email, DateTimeOffset? createdAtOverride = null, CancellationToken clt = default)
        {
            if(string.IsNullOrWhiteSpace(email))
            {
                return ServiceResult<Account>.Failure(ServiceError.InvalidInput);
            }
            DateTimeOffset createdAt = createdAtOverride ?? DateTimeOffset.UtcNow;
            
            await using var transaction = await _appDbContext.Database.BeginTransactionAsync(clt);
            try
            {
                Account newAccount = new()
                {
                    Email = email,
                    CreatedAt = createdAt,
                };
                
                string initialUsername = await _usernameGenerator.GenerateUsername();

                Profile newAccountProfile = new()
                {
                    AccountId = newAccount.AccountId,
                    Username = initialUsername,
                };

                _appDbContext.Accounts.Add(newAccount);
                _appDbContext.Profiles.Add(newAccountProfile);
                await _appDbContext.SaveChangesAsync(clt);

                await transaction.CommitAsync(clt);

                return ServiceResult<Account>.Success(newAccount);
            }
            catch(OperationCanceledException)
            {
                return ServiceResult<Account>.Failure(ServiceError.OperationCancelled);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                _logger.LogError("Unexpected error occurred when creating new account and profile: {message}", ex.Message);
                return ServiceResult<Account>.Failure(ServiceError.DbError);
            }
        }

        public async Task<ServiceResult<Account>> FindAccountByEmailAsync(string email, CancellationToken clt = default)
        {
            if(string.IsNullOrWhiteSpace(email))
            {
                return ServiceResult<Account>.Failure(ServiceError.InvalidInput);
            }
            try
            {
                // IQueryable<Account> query = _appDbContext.Accounts;
                // query = query.Where(a => a.Email == email);
                // Account? foundAccount = await query.FirstOrDefaultAsync(clt);
                Account? foundAccount = await _appDbContext.Accounts
                    // .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Email == email, clt);
                if(foundAccount == null)
                {
                    return ServiceResult<Account>.Failure(ServiceError.NoAccountFound);
                }
                return ServiceResult<Account>.Success(foundAccount);
            }
            catch(OperationCanceledException)
            {
                return ServiceResult<Account>.Failure(ServiceError.OperationCancelled);
            }
            // TODO: Catch other errors maybe too?
        }

        public async Task<ServiceResult<Account>> FindAccountByIdAsync(Guid accountId, CancellationToken clt = default)
        {
            if(accountId == Guid.Empty)
            {
                return ServiceResult<Account>.Failure(ServiceError.InvalidInput);
            }
            try
            {
                // .AsNoTracking()
                Account? foundAccount = await _appDbContext.Accounts
                    .FirstOrDefaultAsync(a => a.AccountId == accountId, clt);
                if(foundAccount == null)
                {
                    return ServiceResult<Account>.Failure(ServiceError.NoAccountFound);
                }
                return ServiceResult<Account>.Success(foundAccount);
            }
            catch(OperationCanceledException)
            {
                return ServiceResult<Account>.Failure(ServiceError.OperationCancelled);
            }
            // TODO: Catch other errors maybe too?
        }

        public async Task<ServiceResult<Unit>> MarkVerifiedEmailAsync(Account account, CancellationToken clt)
        {
            if(account == null)
            {
                return ServiceResult<Unit>.Failure(ServiceError.InvalidInput);
            }
            try
            {
                account.VerifiedAt = DateTimeOffset.UtcNow;
                await _appDbContext.SaveChangesAsync(clt);
                return ServiceResult<Unit>.Success(Unit.Value);
            }
            catch (OperationCanceledException)
            {
                return ServiceResult<Unit>.Failure(ServiceError.OperationCancelled);
            }
            // TODO: Catch other errors maybe too?
        }

        // TODO: Look into if this should be on the entity as business rule(?)
        public async Task<ServiceResult<Unit>> SetPasswordAsync(Account account, string password, CancellationToken clt)
        {
            if(account == null || string.IsNullOrWhiteSpace(password))
            {
                return ServiceResult<Unit>.Failure(ServiceError.InvalidInput);
            }
            try
            {
                account.PasswordHash = _passwordHasher.HashPassword(account, password);
                await _appDbContext.SaveChangesAsync(clt);
                return ServiceResult<Unit>.Success(Unit.Value);
            }
            catch (OperationCanceledException)
            {
                return ServiceResult<Unit>.Failure(ServiceError.OperationCancelled);
            }
            // TODO: Catch other errors maybe too?
        }

        // TODO: Look into if this should be on the entity as business rule(?)
        public async Task<ServiceResult<Unit>> MarkFinishedSignupAsync(Account account, CancellationToken clt)
        {
            if(account == null)
            {
                return ServiceResult<Unit>.Failure(ServiceError.InvalidInput);
            }

            try
            {   
                account.FinishedSignupAt = DateTimeOffset.UtcNow;
                await _appDbContext.SaveChangesAsync(clt);
                return ServiceResult<Unit>.Success(Unit.Value);
            }
            catch(OperationCanceledException)
            {
                return ServiceResult<Unit>.Failure(ServiceError.OperationCancelled);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Unexpected error occurred when marking account {AccountId} as having finished signup: {message}", 
                    account.AccountId, 
                    ex.Message
                );
                return ServiceResult<Unit>.Failure(ServiceError.DbError);
            }
        }
    }
}