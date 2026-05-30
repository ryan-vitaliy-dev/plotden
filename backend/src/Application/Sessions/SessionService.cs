using System.Net;
using Application.Common;
using Application.Common.Interfaces;
using Domain.Common;
using Domain.Sessions;
using Microsoft.EntityFrameworkCore;

namespace Application.Sessions
{
    public class SessionService(IAppDbContext context)
    {
        private readonly IAppDbContext _context = context;

        /*

            + CreateSessionAsync
        + GetSessionByIdAsync
        + InvalidateSessionAsync

        */

        /// <summary>
        /// Creates a new <see cref="Session"/> for an existing <see cref="Account"/>.
        /// </summary>
        /// <param name="accountId">The ID of the account to create a new session for.</param>
        /// <param name="ipAddress">The IP Address of the requesting client that owns the account.</param>
        /// <param name="userAgent">The user agent string of the requesting client that owns the account.</param>
        /// <param name="createdAtOverride">Optional, the <see cref="DateTimeOffset"/> to use instead of the default (<see cref="DateTimeOffset.UtcNow"/>).</param>
        /// <param name="clt">A <see cref="CancellationToken"/> to observe while performing the operation.</param>
        /// <returns>
        /// A <see cref="ServiceResult{T}"/> containing an <see cref="Session"/> if the creation succeeds,
        /// or a failure with an appropriate <see cref="ServiceError"/>.
        /// </returns>
        public async Task<ServiceResult<Session>> CreateSessionAsync(Guid accountId, SessionType sessionType, IPAddress? ipAddress, string? userAgent, DateTimeOffset? createdAtOverride, CancellationToken clt)
        {
            if(accountId == Guid.Empty)
            {
                return ServiceResult<Session>.Failure(ServiceError.InvalidInput);
            }
            try
            {
                DateTimeOffset createdAt = createdAtOverride ?? DateTimeOffset.UtcNow;
                DateTimeOffset expiresAt = createdAt.AddDays(7); // TODO: make session expiration configurable
                Session newSession = new()
                {
                    AccountId = accountId,
                    SessionType = sessionType,
                    CreatedAt = createdAt,
                    ExpiresAt = expiresAt,
                    UserAgent = userAgent,
                    IpAddress = ipAddress,
                };

                await _context.Sessions.AddAsync(newSession, clt);
                await _context.SaveChangesAsync(clt);
                return ServiceResult<Session>.Success(newSession);   
            }
            catch (OperationCanceledException)
            {
                return ServiceResult<Session>.Failure(ServiceError.OperationCancelled);
            }
        }

        public async Task<ServiceResult<Unit>> IsSessionValidAsync(Guid sessionId, CancellationToken clt)
        {
            try
            {
                Session? validSession = await _context.Sessions
                    .FirstOrDefaultAsync(s => 
                        s.SessionId == sessionId &&
                        s.RevokedAt == null &&
                        s.ExpiresAt > DateTimeOffset.UtcNow,
                    clt);

                if(validSession == null)
                {
                    return ServiceResult<Unit>.Failure(ServiceError.InvalidSession);
                }
                return ServiceResult<Unit>.Success(Unit.Value);
            }
            catch (OperationCanceledException)
            {
                return ServiceResult<Unit>.Failure(ServiceError.OperationCancelled);
            }
        }

        public async Task<ServiceResult<Unit>> InvalidateAllSessionsAsync(Guid accountId, CancellationToken clt)
        {
            if(accountId == Guid.Empty)
            {
                return ServiceResult<Unit>.Failure(ServiceError.InvalidInput);
            }
            try
            {
                await _context.Sessions
                    .Where(s => 
                        s.AccountId == accountId &&
                        s.RevokedAt == null
                    )
                    .ExecuteDeleteAsync(clt);
                await _context.SaveChangesAsync(clt);
                return ServiceResult<Unit>.Success(Unit.Value);
            }
            catch (OperationCanceledException)
            {
                return ServiceResult<Unit>.Failure(ServiceError.OperationCancelled);
            }
        }

        public async Task<ServiceResult<Unit>> InvalidAllSessionsExceptAsync(Guid accountId, Guid excludedSessionId, CancellationToken clt)
        {
            if(accountId == Guid.Empty || excludedSessionId == Guid.Empty)
            {
                return ServiceResult<Unit>.Failure(ServiceError.InvalidInput);
            }
            try
            {
                await _context.Sessions
                    .Where(
                        s => s.AccountId == accountId &&
                        s.RevokedAt == null && // not technically necessary to check for right now as RevokedAt for sessions is unused, and thus never set, but may change later
                        s.SessionId != excludedSessionId
                    )
                    .ExecuteDeleteAsync(clt);
                await _context.SaveChangesAsync(clt);
                return ServiceResult<Unit>.Success(Unit.Value);
            }
            catch (OperationCanceledException)
            {
                return ServiceResult<Unit>.Failure(ServiceError.OperationCancelled);
            }
        }
    }
}