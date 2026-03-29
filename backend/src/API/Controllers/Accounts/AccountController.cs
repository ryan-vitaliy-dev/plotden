using backend.API.DTOs.Accounts;
using backend.Application.Accounts.DTOs;

// using backend.Application.Accounts.DTOs;
using backend.Application.Handlers;
using backend.Application.Handlers.Signup;
using backend.Domain.Accounts;


// using backend.Features.Auth.Handlers;
using backend.Infrastructure.Common;
using backend.Infrastructure.Persistence;
using backend.Resources;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace backend.API.Controllers.Accounts
{

    [ApiController]
    [Route("api")]
    // IStringLocalizer<SharedResource> localizer
    public class AccountController(SignupEmailHandler signupHandler, IStringLocalizer<SharedResource> localizer) : ControllerBase
    {
        private readonly SignupEmailHandler _signupHandler = signupHandler;
        private readonly IStringLocalizer<SharedResource> _localizer = localizer;

        // private readonly IStringLocalizer _accountLocalizer = factory.Create(
        //     "Infrastructure.Localization.Resources.Features.Account.ValidationMessages",
        //     typeof(Program).Assembly.GetName().Name!
        // );
        
        [HttpPost("accounts/signup/email")]
        public async Task<IActionResult> SignupAccountEmail([FromBody] AccountSignupEmailDTO dto, CancellationToken clt)
        {
            //System.Net.IPAddress? ipHeader = HttpContext.Connection.RemoteIpAddress;
            // string ipAddress = ipHeader != null ? ipHeader.ToString() : "Unknown";
            // string userAgent = HttpContext.Request.Headers.UserAgent.FirstOrDefault() ?? "Unknown";

            ServiceResult<AccountSignupEmailResult> signupResult = await _signupHandler.HandleAsync(dto, clt);

            if(signupResult.IsFailure)
            {
                return signupResult.ErrorCode switch
                {
                    ServiceError.InvalidInput => BadRequest(new { message = _localizer["GeneralBadRequest"].Value }),
                    ServiceError.OperationCancelled => StatusCode(499),
                    _ => StatusCode(500, new { message = _localizer["GeneralServerError"].Value })
                };
            }

            // Account newAccount = signupResult.Value;
            AccountSignupEmailResult resultData = signupResult.Value;

            // HttpContext.Response.Cookies.Append(
            //     "sid",
            //     resultData.SessionId.ToString(),
            //     new CookieOptions
            //     {
            //         HttpOnly = true,
            //         Secure = true,
            //         SameSite = SameSiteMode.Lax,
            //         Expires = resultData.ExpiresAt
            //     }
            // );

            return StatusCode(201, new { 
                message = _localizer["Signup_VerificationEmailSent"].Value, 
                provided_email = resultData.Email
            });
        }

        // [HttpPatch("accounts/signup/verify")]
        // public async Task<IActionResult> VerifyAccountEmail([FromQuery] AccountSignupVerifyDTO dto, CancellationToken clt) 
        // {
            
        // }

        //[HttpPatch("accounts/signup/password")] - handles setting password on signup
        //[HttpPatch("accounts/settings/email")] - handles updating email
        //[HttpPatch("accounts/settings/password")] - handles updating password later

        //[HttpPatch("accounts/profiles/username")] - handles updating username
    }
}