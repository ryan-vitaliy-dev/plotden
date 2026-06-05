using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Localization;

using Asp.Versioning;

using API.DTOs.Auth;
using API.Filters;
using Application.Common;
using Application.Handlers.Auth;
using Application.Resources;
using Domain.Common;
using Application.Sessions.Results;

namespace API.Controllers.Auth
{
    [ApiController]
    [ApiVersion(1)]
    [Route("api/v{version:apiVersion}/auth")]
    public class AuthController(
        IStringLocalizer<SharedResource> localizer,
        SignupRequestEmailHandler signupRequestEmailHandler,
        SignupVerifyEmailHandler signupVerifyEmailHandler,
        SignupResumeSessionHandler signupResumeSessionHandler,
        SignupSetPasswordHandler signupSetPasswordHandler,
        SigninHandler signinHandler,
        SigninRequestRecoveryHandler signinRequestRecoveryHandler,
        SigninConsumePasswordResetHandler signinConsumePasswordResetHandler,
        SigninSetPasswordResetHandler signinSetPasswordResetHandler
    ) : ControllerBase
    {
        private readonly IStringLocalizer<SharedResource> _localizer = localizer;
        private readonly SignupRequestEmailHandler _signupRequestEmailHandler = signupRequestEmailHandler;
        private readonly SignupVerifyEmailHandler _signupVerifyEmailHandler = signupVerifyEmailHandler;
        private readonly SignupResumeSessionHandler _signupResumeSessionHandler = signupResumeSessionHandler;
        private readonly SignupSetPasswordHandler _signupSetPasswordHandler = signupSetPasswordHandler;
        private readonly SigninHandler _signinHandler = signinHandler;
        private readonly SigninRequestRecoveryHandler _signinRequestRecoveryHandler = signinRequestRecoveryHandler;
        private readonly SigninConsumePasswordResetHandler _signinConsumePasswordResetHandler = signinConsumePasswordResetHandler;
        private readonly SigninSetPasswordResetHandler _signinSetPasswordResetHandler = signinSetPasswordResetHandler;

        private static readonly HashSet<ServiceError> _signupRecoverErrors =
        [
            ServiceError.NoAccountFound,
            ServiceError.AccountNotVerified
        ];


        [HttpPost("signup/email")]
        [BlockIfAuthenticated]
        public async Task<IActionResult> SignupRequestEmail([FromBody] SignupEmailDTO dto, CancellationToken clt)
        {
            ServiceResult<Unit> signupResult = await _signupRequestEmailHandler.HandleAsync(dto.Email, clt);

            if(signupResult.IsFailure)
            {
                return signupResult.ErrorCode switch
                {
                    ServiceError.InvalidInput => BadRequest(new { message = _localizer["General_Error_400BadRequest"].Value }),
                    ServiceError.OperationCancelled => StatusCode(499),
                    _ => StatusCode(500, new { message = _localizer["General_Error_500Server"].Value })
                };
            }
            return StatusCode(201, new { 
                message = _localizer["Signup_VerificationEmailSent"].Value, 
                provided_email = dto.Email
            });
        }


        [HttpGet("signup/verify")]
        [BlockIfAuthenticated]
        public async Task<IActionResult> SignupVerifyEmail([FromQuery] SignupVerifyDTO dto, CancellationToken clt)
        {
            ClientInfo clientInfo = new(HttpContext.Connection.RemoteIpAddress, HttpContext.Request.Headers.UserAgent.First());

            ServiceResult<CreatedSession> verifySignupResult = await _signupVerifyEmailHandler.HandleAsync(dto.Token, clientInfo, clt);

            if(verifySignupResult.IsFailure)
            {
                return verifySignupResult.ErrorCode switch
                {
                    ServiceError.InvalidInput or ServiceError.NoTokenFound => BadRequest(new { message = _localizer["General_Error_InvalidToken"].Value }),
                    ServiceError.OperationCancelled => StatusCode(499),
                    _ => StatusCode(500, new { message = _localizer["General_Error_500Server"].Value })
                };
            }
            CreatedSession resultData = verifySignupResult.Value;

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

            return Ok(new 
            { 
                message = _localizer["Signup_Success_EmailVerified"].Value
            });
        }

        [HttpGet("signup/resume")]
        [BlockIfAuthenticated]
        public async Task<IActionResult> SignupResumeSession([FromQuery] SignupResumeDTO dto, CancellationToken clt)
        {
            ClientInfo clientInfo = new(HttpContext.Connection.RemoteIpAddress, HttpContext.Request.Headers.UserAgent.First());

            ServiceResult<CreatedSession> resumeSignupResult = await _signupResumeSessionHandler.HandleAsync(dto.Token, clientInfo, clt);

            if(resumeSignupResult.IsFailure)
            {
                return resumeSignupResult.ErrorCode switch
                {
                    ServiceError.InvalidInput or ServiceError.NoTokenFound => BadRequest(new { message = _localizer["General_Error_InvalidToken"].Value }),
                    ServiceError.OperationCancelled => StatusCode(499),
                    _ => StatusCode(500, new { message = _localizer["General_Error_500Server"].Value })
                };
            }
            CreatedSession resultData = resumeSignupResult.Value;

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

            return Ok(new 
            { 
                message = _localizer["Recover_Success_SetNew"].Value
            });
        }

        [HttpPatch("signup/password")]
        [Authorize(Policy = "IncompleteSignupSession")]
        public async Task<IActionResult> SignupSetPassword(SignupPasswordDTO dto, CancellationToken clt)
        {
            Guid accountIdFromSession = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            ClientInfo clientInfo = new(HttpContext.Connection.RemoteIpAddress, HttpContext.Request.Headers.UserAgent.First());

            ServiceResult<CreatedSession> passwordSetResult = await _signupSetPasswordHandler.HandleAsync(accountIdFromSession, dto.Password, clientInfo, clt);

            if(passwordSetResult.IsFailure)
            {
                return passwordSetResult.ErrorCode switch
                {
                    ServiceError.InvalidInput => BadRequest(new { message = _localizer["General_Error_400BadRequest"].Value }),
                    ServiceError.AccountNotVerified => StatusCode(403, new { message = _localizer["Signup_Error_EmailNotVerified"].Value }),
                    ServiceError.PasswordAlreadySet => Conflict(new { message = _localizer["Signup_Error_PasswordAlreadySet"].Value }),
                    ServiceError.OperationCancelled => StatusCode(499),
                    _ => StatusCode(500, new { message = _localizer["General_Error_500Server"].Value })
                };
            }

            CreatedSession resultData = passwordSetResult.Value;

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

            return Ok(new
            {
                message = _localizer["Signup_Success_PasswordSet"].Value
            });
        }


        [HttpPost("signin")]
        [BlockIfAuthenticated]
        public async Task<IActionResult> Signin(SigninDTO dto, CancellationToken clt)
        {
            ClientInfo clientInfo = new(HttpContext.Connection.RemoteIpAddress, HttpContext.Request.Headers.UserAgent.First());

            ServiceResult<CreatedSession> signinResult = await _signinHandler.HandleAsync(dto.Email, dto.Password, clientInfo, clt);

            if(signinResult.IsFailure)
            {
                return signinResult.ErrorCode switch
                {
                    ServiceError.InvalidInput => BadRequest(new { message = _localizer["General_Error_InvalidToken"].Value }),
                    ServiceError.InvalidCredentials => Unauthorized(new { message = _localizer["Signin_Error_InvalidEmailOrPassword"].Value }),
                    ServiceError.OperationCancelled => StatusCode(499),
                    _ => StatusCode(500, new { message = _localizer["General_Error_500Server"].Value })
                };
            }
            CreatedSession resultData = signinResult.Value;

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

            return Ok(new 
            { 
                message = _localizer["Signin_Success"].Value
            });
        }

        [HttpPost("signin/recover")]
        [BlockIfAuthenticated]
        public async Task<IActionResult> SigninRequestRecovery(SigninRecoverDTO dto, CancellationToken clt)
        {
            ServiceResult<Unit> recoverResult = await _signinRequestRecoveryHandler.HandleAsync(dto.Email, clt);

            if(recoverResult.IsFailure && !_signupRecoverErrors.Contains(recoverResult.ErrorCode!.Value))
            {
                return StatusCode(500, new { message = _localizer["General_Error_500Server"].Value });
            }
            return Ok(new
            {
                message = _localizer["Recover_Success_EmailSent"].Value, 
                provided_email = dto.Email
            });
        }

        [HttpGet("reset-password")] 
        [BlockIfAuthenticated]
        public async Task<IActionResult> SigninConsumePasswordReset([FromQuery] SigninResetPasswordDTO dto, CancellationToken clt)
        {
            ClientInfo clientInfo = new(HttpContext.Connection.RemoteIpAddress, HttpContext.Request.Headers.UserAgent.First());

            ServiceResult<CreatedSession> resetPasswordResult = await _signinConsumePasswordResetHandler.HandleAsync(dto.Token, clientInfo, clt);

            if(resetPasswordResult.IsFailure)
            {
                return resetPasswordResult.ErrorCode switch
                {
                    ServiceError.InvalidInput or ServiceError.NoTokenFound => BadRequest(new { message = _localizer["General_Error_InvalidToken"].Value }),
                    ServiceError.OperationCancelled => StatusCode(499),
                    _ => StatusCode(500, new { message = _localizer["General_Error_500Server"].Value })
                };
            }
            CreatedSession resultData = resetPasswordResult.Value;

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

            return Ok(new 
            { 
                message = _localizer["Recover_Success_SetNew"].Value
            });
        }

        [HttpPatch("reset-password")]
        [Authorize(Policy = "PasswordResetSession")]
        public async Task<IActionResult> SigninSetPasswordReset(SignupPasswordDTO dto, CancellationToken clt)
        {
            Guid accountIdFromSession = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            ClientInfo clientInfo = new(HttpContext.Connection.RemoteIpAddress, HttpContext.Request.Headers.UserAgent.First());

            ServiceResult<CreatedSession> applyPasswordResult = await _signinSetPasswordResetHandler.HandleAsync(accountIdFromSession, dto.Password, clientInfo, clt);

            if(applyPasswordResult.IsFailure)
            {
                return applyPasswordResult.ErrorCode switch
                {
                    ServiceError.InvalidInput => BadRequest(new { message = _localizer["General_Error_400BadRequest"].Value }),
                    ServiceError.AccountNotVerified => StatusCode(403, new { message = _localizer["Signup_Error_EmailNotVerified"].Value }),
                    ServiceError.PasswordAlreadySet => Conflict(new { message = _localizer["Signup_Error_PasswordAlreadySet"].Value }),
                    ServiceError.OperationCancelled => StatusCode(499),
                    _ => StatusCode(500, new { message = _localizer["General_Error_500Server"].Value })
                };
            }

            CreatedSession resultData = applyPasswordResult.Value;

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

            return Ok(new
            {
                message = _localizer["Recover_Success_PasswordReset"].Value
            });
        }
    }
}