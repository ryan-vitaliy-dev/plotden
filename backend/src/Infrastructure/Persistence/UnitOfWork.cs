using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Persistence
{
    public class UnitOfWork(AppDbContext dbContext) : IUnitOfWork
    {
        private readonly AppDbContext _dbContext = dbContext;

        public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken clt)
        {
            return await _dbContext.Database.BeginTransactionAsync(clt);
        }
    }
}