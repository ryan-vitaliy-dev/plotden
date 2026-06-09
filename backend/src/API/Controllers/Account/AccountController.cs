using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Authorization;

using Asp.Versioning;

using Application.Common;
using Application.Handlers.Accounts;
using Application.Resources;
using Domain.Common;
using API.DTOs.Account;
using API.DTOs.Common;

namespace API.Controllers.Account
{

    [ApiController]
    [ApiVersion(1)]
    [Route("api/v{version:apiVersion}/account")]

    public class AccountController(
        RequestEmailUpdateHandler requestEmailUpdateHandler,
        ConfirmEmailUpdateHandler confirmEmailUpdateHandler,
        UpdatePasswordHandler updatePasswordHandler,
        IStringLocalizer<SharedResource> localizer
    ) : ControllerBase
    {
        private readonly RequestEmailUpdateHandler _requestEmailUpdateHandler = requestEmailUpdateHandler;
        private readonly ConfirmEmailUpdateHandler _confirmEmailUpdateHandler = confirmEmailUpdateHandler;
        private readonly UpdatePasswordHandler _updatePasswordHandler = updatePasswordHandler;
        private readonly IStringLocalizer<SharedResource> _localizer = localizer;


        [HttpPatch("password")]
        [Authorize(Policy = "StandardSession")]
        public async Task<IActionResult> UpdatePassword(UpdatePasswordDTO dto, CancellationToken clt)
        {
            Guid sessionId = Guid.Parse(User.FindFirst("SessionId")!.Value);
            Guid accountId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            ServiceResult<Unit> updatePasswordResult = await _updatePasswordHandler.HandleAsync(
                accountId, sessionId, dto.CurrentPassword, dto.NewPassword, clt);

            if(updatePasswordResult.IsFailure)
            {
                return updatePasswordResult.ErrorCode switch
                {
                    ServiceError.InvalidInput or 
                    ServiceError.InvalidCredentials or 
                    ServiceError.PasswordNotSet => StatusCode(401, new { message = _localizer["Account_UpdatePassword_Error_InvalidCurrent"].Value }),
                    ServiceError.OperationCancelled => StatusCode(499),
                    _ => StatusCode(500, new { message = _localizer["General_Error_500Server"].Value })
                };
            }
            return Ok(new
            {
                message = _localizer["Account_UpdatePassword_Success"].Value
            });
        }

        [HttpPost("email")]
        [Authorize(Policy = "StandardSession")]
        public async Task<IActionResult> RequestEmailUpdate(UpdateEmailDTO dto, CancellationToken clt)
        {
            Guid accountId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            ServiceResult<Unit> requestEmailUpdateResult = await _requestEmailUpdateHandler.HandleAsync(accountId, dto.NewEmail, dto.Password, clt);

            if(requestEmailUpdateResult.IsFailure)
            {
                return requestEmailUpdateResult.ErrorCode switch
                {
                    ServiceError.InvalidInput or
                    ServiceError.InvalidCredentials or 
                    ServiceError.PasswordNotSet => StatusCode(401, new { message = _localizer["Account_UpdateEmail_Error_InvalidPassword"].Value }),
                    ServiceError.OperationCancelled => StatusCode(499),
                    _ => StatusCode(500, new { message = _localizer["General_Error_500Server"].Value })
                };
            }
            return Ok(new
            {
                message = _localizer["Account_UpdateEmail_EmailSent"].Value
            });
        }

        [HttpPost("email/confirm")]
        // TODO: consider authorization stuff
        public async Task<IActionResult> ConfirmEmailUpdate([FromBody] EmailTokenDTO dto, CancellationToken clt)
        {
            ServiceResult<Unit> confirmUpdateEmailResult = await _confirmEmailUpdateHandler.HandleAsync(dto.Token, clt);
            if(confirmUpdateEmailResult.IsFailure)
            {
                return confirmUpdateEmailResult.ErrorCode switch
                {
                    ServiceError.InvalidInput => StatusCode(400, new { message = _localizer["General_Error_InvalidToken"].Value }),
                    ServiceError.OperationCancelled => StatusCode(499),
                    _ => StatusCode(500, new { message = _localizer["General_Error_500Server"].Value }) // TODO: Check these to make sure it meets full scope of error handling
                };
            }
            return Ok(new
            {
                message = _localizer["Account_UpdateEmail_Success"].Value
            });
        }
    }
}