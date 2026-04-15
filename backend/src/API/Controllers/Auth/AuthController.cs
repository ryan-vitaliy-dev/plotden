using System.Net;
using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Localization;

using API.DTOs.Auth;
using Application.Common;
using Application.Auth.Results;
using Application.Handlers.Signup;
using Application.Resources;
using Domain.Common;

namespace API.Controllers.Auth
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController(
        SignupEmailHandler signupEmailHandler,
        SignupVerifyHandler signupVerifyHandler,
        SignupPasswordHandler signupPasswordHandler,
        SigninRecoverHandler signinRecoverHandler,
        IStringLocalizer<SharedResource> localizer
    ) : ControllerBase
    {
        private readonly SignupEmailHandler _signupEmailHandler = signupEmailHandler;
        private readonly SignupVerifyHandler _signupVerifyHandler = signupVerifyHandler;
        private readonly SignupPasswordHandler _signupPasswordHandler = signupPasswordHandler;
        private readonly SigninRecoverHandler _signinRecoverHandler = signinRecoverHandler;
        private readonly IStringLocalizer<SharedResource> _localizer = localizer;



        [HttpPost("signup/email")]
        // TODO: Figure out if I need to block request if a session is present
        public async Task<IActionResult> SignupEmail([FromBody] SignupEmailDTO dto, CancellationToken clt)
        {
            ServiceResult<SignupEmailResult> signupResult = await _signupEmailHandler.HandleAsync(dto.Email, clt);

            if(signupResult.IsFailure)
            {
                return signupResult.ErrorCode switch
                {
                    ServiceError.InvalidInput => BadRequest(new { message = _localizer["GeneralBadRequest"].Value }),
                    ServiceError.OperationCancelled => StatusCode(499),
                    _ => StatusCode(500, new { message = _localizer["GeneralServerError"].Value })
                };
            }
            SignupEmailResult resultData = signupResult.Value;
            return StatusCode(201, new { 
                message = _localizer["Signup_VerificationEmailSent"].Value, 
                provided_email = resultData.Email
            });
        }


        [HttpGet("signup/verify")]
        // TODO: Figure out if I need to block request if a session is present
        public async Task<IActionResult> SignupVerify([FromQuery] SignupVerifyDTO dto, CancellationToken clt)
        {
            IPAddress? ipAddress = HttpContext.Connection.RemoteIpAddress;
            string? userAgent = HttpContext.Request.Headers.UserAgent.First();

            ServiceResult<SignupVerifyResult> verifySignupResult = await _signupVerifyHandler.HandleAsync(dto.Token, ipAddress, userAgent, clt);

            if(verifySignupResult.IsFailure)
            {
                return verifySignupResult.ErrorCode switch
                {
                    ServiceError.InvalidInput or ServiceError.NoTokenFound => BadRequest(new { message = _localizer["Signup_Error_InvalidToken"].Value }),
                    ServiceError.OperationCancelled => StatusCode(499),
                    _ => StatusCode(500, new { message = _localizer["GeneralServerError"].Value })
                };
            }
            SignupVerifyResult resultData = verifySignupResult.Value;

            HttpContext.Response.Cookies.Append(
                "sid",
                resultData.SessionId,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = resultData.ExpiresAt
                }
            );

            return Ok(new { 
                message = _localizer["Signup_Success_EmailVerified"].Value
            });
        }

        [HttpPatch("signup/password")]
        [Authorize(Policy = "ValidSession")]
        public async Task<IActionResult> SignupPassword(SignupPasswordDTO dto, CancellationToken clt)
        {
            Guid accountIdFromSession = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            ServiceResult<Unit> passwordSetResult = await _signupPasswordHandler.HandleAsync(accountIdFromSession, dto.Password, clt);
            if(passwordSetResult.IsFailure)
            {
                return passwordSetResult.ErrorCode switch
                {
                    ServiceError.InvalidInput => BadRequest(new { message = _localizer["GeneralBadRequest"].Value }),
                    ServiceError.AccountNotVerified => StatusCode(403, new { message = _localizer["Signup_Error_EmailNotVerified"].Value }),
                    ServiceError.PasswordAlreadySet => Conflict(new { message = _localizer["Signup_Error_PasswordAlreadySet"].Value }),
                    ServiceError.OperationCancelled => StatusCode(499),
                    _ => StatusCode(500, new { message = _localizer["GeneralServerError"].Value })
                };
            }

            return Ok(new
            {
                message = _localizer["Signup_Success_PasswordSet"].Value
            });
        }


        [HttpPost("signin/recover")]
        // TODO: Figure out if I need to block request if a session is present
        public async Task<IActionResult> SigninRecover(SigninRecoverDTO dto, CancellationToken clt)
        {
            // Call recovery handler
            // Recovery handler figures out account status and what to send for email
            ServiceResult<Unit> recoverResult = await _signinRecoverHandler.HandleAsync(dto.Email, clt);
            if(recoverResult.IsFailure)
            {
                // TODO
                throw new NotImplementedException();
            }
            return StatusCode(201, new
            {
                message = _localizer["Recover_Success_EmailSent"].Value, 
                provided_email = dto.Email
            });
        }



        // [HttpPost("signin")]
        // public async Task<IActionResult> SignIn()
        // {
        //     throw new NotImplementedException();
        // }

        // [HttpPost("signin/recover")]
        // public async Task<IActionResult> SignInRecover()
        // {
        //     throw new NotImplementedException();
        // }

        // [HttpGet("signin/reset-password")]
        // public async Task<IActionResult> SignInForgotPassword()
        // {
        //     throw new NotImplementedException();
        // }
    }
}