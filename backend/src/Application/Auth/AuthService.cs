using Application.Accounts;
using Application.Auth.DTOs;
using Application.Common;
using Application.Common.Interfaces;
using Application.Tokens;
using Domain.Accounts;
using Domain.Common;
using Domain.Tokens;
using Microsoft.Extensions.Logging;

namespace Application.Auth
{
    public class AuthService(IAppDbContext appDbContext, TokenService tokenService, AccountService accountService, ILogger<AuthService> logger)
    {
        private readonly IAppDbContext _appDbContext = appDbContext;

        private readonly TokenService _tokenService = tokenService;

        private readonly AccountService _accountService = accountService;

        private readonly ILogger<AuthService> _logger = logger;

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
    }
}