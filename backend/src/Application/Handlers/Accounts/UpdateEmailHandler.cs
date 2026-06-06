using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using Application.Accounts;
using Application.Common;
using Application.Sessions;
using Domain.Accounts;
using Domain.Common;
using Application.Common.Interfaces;

namespace Application.Handlers.Accounts
{
    public class UpdateEmailHandler(
        ILogger<UpdateEmailHandler> logger,
        IUnitOfWork unitOfWork,
        AccountService accountService, 
        SessionService sessionService 
    )
    {
        private readonly ILogger<UpdateEmailHandler> _logger = logger;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        private readonly AccountService _accountService = accountService;
        private readonly SessionService _sessionService = sessionService;
        
        private readonly PasswordHasher<Account> _passwordHasher = new();

        public async Task<ServiceResult<Unit>> HandleAsync(Guid accountId, Guid currentSessionId, string newEmail, string password, CancellationToken clt)
        {
            if (string.IsNullOrWhiteSpace(newEmail) || string.IsNullOrWhiteSpace(password))
            {
                return ServiceResult<Unit>.Failure(ServiceError.InvalidInput);
            }
            
            ServiceResult<Account> findAccountResult = await _accountService.FindAccountByIdAsync(accountId, clt);
            if(findAccountResult.IsFailure)
            {
                return ServiceResult<Unit>.Failure(ServiceError.NoAccountFound);
            }
            Account account = findAccountResult.Value;


            // TODO: handle rehash stuff later if needed

            await using var tx = await _unitOfWork.BeginTransactionAsync(clt);
            
            // TODO: Logic for setting the new email, deleting emailupdaterequest row, etc.

            await tx.CommitAsync(clt);

            return ServiceResult<Unit>.Success(Unit.Value);
        }
    }
}