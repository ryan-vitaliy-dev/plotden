using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Application.Common;
using Application.Common.Interfaces;
using Domain.Accounts;
using Domain.Common;

namespace Application.Accounts
{
    public class EmailUpdateRequestService(IAppDbContext appDbContext, ILogger<EmailUpdateRequestService> logger)
    {
        private readonly IAppDbContext _appDbContext = appDbContext;

        private readonly ILogger<EmailUpdateRequestService> _logger = logger;

        public async Task<ServiceResult<Unit>> CreateEmailUpdateRequestAsync(
            Guid accountId, 
            string oldEmail, 
            string newEmail, 
            DateTimeOffset? createdAtOverride = null, 
            CancellationToken clt = default
        )
        {
            if(accountId == Guid.Empty)
            {
                return ServiceResult<Unit>.Failure(ServiceError.InvalidInput);
            }
            DateTimeOffset createdAt = createdAtOverride ?? DateTimeOffset.UtcNow;
            try
            {
                EmailUpdateRequest newEmailUpdateRequest = new()
                {
                    AccountId = accountId,
                    OldEmail = oldEmail,
                    NewEmail = newEmail,
                    RequestedAt = createdAt
                };
                _appDbContext.EmailUpdateRequests.Add(newEmailUpdateRequest);

                await _appDbContext.SaveChangesAsync(clt);

                return ServiceResult<Unit>.Success(Unit.Value);
            }
            catch (OperationCanceledException)
            {
                return ServiceResult<Unit>.Failure(ServiceError.OperationCancelled);
            }
            // TODO: Catch other errors maybe too?
        }

        public async Task<ServiceResult<EmailUpdateRequest>> FindEmailUpdateRequestByAccountIdAsync(Guid accountId, CancellationToken clt)
        {
            if(accountId == Guid.Empty)
            {
                return ServiceResult<EmailUpdateRequest>.Failure(ServiceError.InvalidInput);
            }
            try
            {
                EmailUpdateRequest? foundEmailUpdateRequest = await _appDbContext.EmailUpdateRequests
                    .FirstOrDefaultAsync(eur => eur.AccountId == accountId, clt);
                if(foundEmailUpdateRequest == null)
                {
                    return ServiceResult<EmailUpdateRequest>.Failure(ServiceError.NoAccountFound);
                }
                return ServiceResult<EmailUpdateRequest>.Success(foundEmailUpdateRequest);
            }
            catch (OperationCanceledException)
            {
                return ServiceResult<EmailUpdateRequest>.Failure(ServiceError.OperationCancelled);
            }
            // TODO: Catch other errors maybe too?
        }

        public async Task<ServiceResult<Unit>> DeleteEmailUpdateRequestAsync(Guid accountId, CancellationToken clt)
        {
            if(accountId == Guid.Empty)
            {
                // TODO: Should I return success here?
                return ServiceResult<Unit>.Failure(ServiceError.InvalidInput);
            }
            try
            {
                await _appDbContext.EmailUpdateRequests
                    .Where(eur => eur.AccountId == accountId)
                    .ExecuteDeleteAsync(clt);
                return ServiceResult<Unit>.Success(Unit.Value);
            }
            catch(OperationCanceledException)
            {
                return ServiceResult<Unit>.Failure(ServiceError.OperationCancelled);
            }
            // TODO: Catch other errors maybe too?
        }
    }
}