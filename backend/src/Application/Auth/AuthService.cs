using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using Application.Common;
using Application.Common.Interfaces;
using Domain.Accounts;
using Domain.Common;
using Domain.Tokens;

namespace Application.Auth
{
    public class AuthService(IAppDbContext appDbContext, ILogger<AuthService> logger)
    {
        private readonly IAppDbContext _appDbContext = appDbContext;

        private readonly ILogger<AuthService> _logger = logger;

        private readonly PasswordHasher<Account> _passwordHasher = new();

        public async Task<ServiceResult<Unit>> ConsumeTokenAndVerifyAccountAsync(Token token, Account account, CancellationToken clt)
        {
            await using var transaction = await _appDbContext.Database.BeginTransactionAsync(clt);
            try
            {
                token.ConsumedAt = DateTimeOffset.UtcNow;
                account.VerifiedAt = DateTime.UtcNow;
                _appDbContext.Tokens.Update(token);
                _appDbContext.Accounts.Update(account);

                await _appDbContext.SaveChangesAsync(clt);
                await transaction.CommitAsync(clt);

                return ServiceResult<Unit>.Success(Unit.Value);
            }
            catch(OperationCanceledException)
            {
                return ServiceResult<Unit>.Failure(ServiceError.OperationCancelled);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                _logger.LogError("Unexpected error occurred when consuming token and marking account as verified: {message}", ex.Message);
                return ServiceResult<Unit>.Failure(ServiceError.DbError);
            }
        }

        public async Task<ServiceResult<Unit>> ConsumeTokenAndResumeAccountSignupAsync(Token token, Account account, CancellationToken clt)
        {
            try
            {
                token.ConsumedAt = DateTimeOffset.UtcNow;
                _appDbContext.Tokens.Update(token);
                await _appDbContext.SaveChangesAsync(clt);

                return ServiceResult<Unit>.Success(Unit.Value);
            }
            catch(OperationCanceledException)
            {
                return ServiceResult<Unit>.Failure(ServiceError.OperationCancelled);
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected error occurred when consuming token and marking account as verified: {message}", ex.Message);
                return ServiceResult<Unit>.Failure(ServiceError.DbError);
            }
        }

        public async Task<ServiceResult<Unit>> FinishAccountSignupAsync(Account account, string password, CancellationToken clt)
        {
            if(account == null || string.IsNullOrWhiteSpace(password))
            {
                return ServiceResult<Unit>.Failure(ServiceError.InvalidInput);
            }

            await using var transaction = await _appDbContext.Database.BeginTransactionAsync(clt);
            try
            {                
                account.PasswordHash = _passwordHasher.HashPassword(account, password);
                account.FinishedSignupAt = DateTimeOffset.UtcNow;
                _appDbContext.Accounts.Update(account);

                await _appDbContext.SaveChangesAsync(clt);
                await transaction.CommitAsync(clt);

                return ServiceResult<Unit>.Success(Unit.Value);
            }
            catch(OperationCanceledException)
            {
                return ServiceResult<Unit>.Failure(ServiceError.OperationCancelled);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                _logger.LogError("Unexpected error occurred when finishing signup for account {AccountId}: {message}", account.AccountId, ex.Message);
                return ServiceResult<Unit>.Failure(ServiceError.DbError);
            }
        }
    }
}