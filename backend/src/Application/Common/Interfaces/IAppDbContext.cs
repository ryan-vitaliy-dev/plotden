using Domain.Accounts;
using Domain.Profiles;
using Domain.Sessions;
using Domain.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Application.Common.Interfaces
{
    public interface IAppDbContext
    {
        DbSet<Account> Accounts { get ; }
        DbSet<Profile> Profiles { get ; }
        DbSet<Session> Sessions { get ; }
        DbSet<Token> Tokens { get ; }

        Task<int> SaveChangesAsync(CancellationToken clt);
        DatabaseFacade Database { get ; }

    }
}