using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Authorization;

using Asp.Versioning;

using API.DTOs.Accounts;
using Application.Common;
using Application.Handlers.Accounts;
using Application.Resources;
using Domain.Common;

namespace API.Controllers.Accounts
{

    [ApiController]
    [ApiVersion(1)]
    [Route("api/v{version:apiVersion}/accounts")]

    public class AccountController(
        UpdatePasswordHandler updatePasswordHandler,
        IStringLocalizer<SharedResource> localizer
    ) : ControllerBase
    {
        private readonly UpdatePasswordHandler _updatePasswordHandler = updatePasswordHandler;
        private readonly IStringLocalizer<SharedResource> _localizer = localizer;


        [HttpPatch("settings/password")]
        [Authorize(Policy = "StandardSession")]
        public async Task<IActionResult> UpdatePassword(UpdatePasswordDTO dto, CancellationToken clt)
        {
            Guid sessionIdFromClaims = Guid.Parse(User.FindFirst("SessionId")!.Value);
            Guid accountIdFromSession = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            ServiceResult<Unit> updatePasswordResult = await _updatePasswordHandler.HandleAsync(
                accountIdFromSession, sessionIdFromClaims, dto.CurrentPassword, dto.NewPassword, clt);

            if(updatePasswordResult.IsFailure)
            {
                return updatePasswordResult.ErrorCode switch
                {
                    ServiceError.InvalidInput => BadRequest(new { message = _localizer["Settings_UpdatePassword_Error_Invalid"].Value }),
                    ServiceError.InvalidCredentials => Unauthorized(new { message = _localizer["Settings_UpdatePassword_Error_InvalidCurrent"].Value }),
                    ServiceError.PasswordNotSet => BadRequest(new { message = _localizer["Settings_UpdatePassword_Error_NotSet"].Value }),
                    ServiceError.OperationCancelled => StatusCode(499),
                    _ => StatusCode(500, new { message = _localizer["General_Error_500Server"].Value })
                };
            }
            return Ok(new
            {
                message = _localizer["Settings_UpdatePassword_Success"].Value
            });
        }
    }
}