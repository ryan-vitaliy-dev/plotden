using Microsoft.Extensions.Logging;

using Application.Sessions;
using Domain.Common;
using Application.Common;

namespace Application.Handlers.Auth
{
    public class SignoutHandler(
        ILogger<SigninHandler> logger,
        SessionService sessionService
    )
    {
        private readonly ILogger<SigninHandler> _logger = logger;
        private readonly SessionService _sessionService = sessionService;

        public async Task<ServiceResult<Unit>> HandleAsync(Guid sessionId, CancellationToken clt)
        {
            ServiceResult<Unit> invalidateSessionResult = await _sessionService.InvalidateSessionAsync(sessionId, clt);
            if(invalidateSessionResult.IsFailure)
            {
                _logger.LogError(
                    "Failed to invalidate session with id {SessionId} - Error: {ErrorCode}",
                    sessionId,
                    invalidateSessionResult.ErrorCode
                );
                return ServiceResult<Unit>.Failure(invalidateSessionResult.ErrorCode!.Value); // TODO: Check if this is ok
            }
            return ServiceResult<Unit>.Success(Unit.Value);
        }
    }
}