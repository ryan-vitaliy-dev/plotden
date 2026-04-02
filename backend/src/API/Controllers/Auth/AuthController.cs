using System.Net;
using backend.API.DTOs.Auth;
using backend.Application.Auth.DTOs;
using backend.Application.Handlers.Signup;
using backend.Infrastructure.Common;
using backend.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace backend.API.Controllers.Auth
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController(
        SignupEmailHandler signupEmailHandler,
        SignupVerifyHandler signupVerifyHandler,
        IStringLocalizer<SharedResource> localizer
    ) : ControllerBase
    {
        private readonly SignupEmailHandler _signupEmailHandler = signupEmailHandler;
        private readonly SignupVerifyHandler _signupVerifyHandler = signupVerifyHandler;
        private readonly IStringLocalizer<SharedResource> _localizer = localizer;


        [HttpPost("signup/email")]
        public async Task<IActionResult> SignupEmail([FromBody] SignupEmailDTO dto, CancellationToken clt)
        {
            ServiceResult<SignupEmailResult> signupResult = await _signupEmailHandler.HandleAsync(dto, clt);

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
        public async Task<IActionResult> SignupVerify([FromQuery] SignupVerifyDTO dto, CancellationToken clt)
        {
            IPAddress? ipAddress = HttpContext.Connection.RemoteIpAddress;
            string? userAgent = HttpContext.Request.Headers.UserAgent.First();

            ServiceResult<SignupVerifyResult> verifySignupResult = await _signupVerifyHandler.HandleAsync(dto, ipAddress, userAgent, clt);

            if(verifySignupResult.IsFailure)
            {
                return verifySignupResult.ErrorCode switch
                {
                    ServiceError.InvalidInput or ServiceError.NoTokenFound => BadRequest(new { message = _localizer["Signup_ErrorInvalidToken"].Value }),
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
                message = _localizer["Signup_SuccessEmailVerified"].Value
            });
        }

        // [HttpPatch("signup/password")]
        // public async Task<IActionResult> SignupPassword()
        // {
        //     throw new NotImplementedException();
        // }

        // [HttpGet("signup/resume")]
        // public async Task<IActionResult> SignupResume()
        // {
        //     throw new NotImplementedException();
        // }



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