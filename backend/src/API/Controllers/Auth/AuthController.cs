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
        SigninVerifyPasswordResetHandler signinVerifyPasswordResetHandler,
        SigninSetPasswordResetHandler signinSetPasswordResetHandler,
        SignoutHandler signoutHandler
    ) : ControllerBase
    {
        private readonly IStringLocalizer<SharedResource> _localizer = localizer;
        private readonly SignupRequestEmailHandler _signupRequestEmailHandler = signupRequestEmailHandler;
        private readonly SignupVerifyEmailHandler _signupVerifyEmailHandler = signupVerifyEmailHandler;
        private readonly SignupResumeSessionHandler _signupResumeSessionHandler = signupResumeSessionHandler;
        private readonly SignupSetPasswordHandler _signupSetPasswordHandler = signupSetPasswordHandler;
        private readonly SigninHandler _signinHandler = signinHandler;
        private readonly SigninRequestRecoveryHandler _signinRequestRecoveryHandler = signinRequestRecoveryHandler;
        private readonly SigninVerifyPasswordResetHandler _signinVerifyPasswordResetHandler = signinVerifyPasswordResetHandler;
        private readonly SigninSetPasswordResetHandler _signinSetPasswordResetHandler = signinSetPasswordResetHandler;

        private readonly SignoutHandler _signoutHandler = signoutHandler;

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


        [HttpPost("signup/verify")]
        [BlockIfAuthenticated]
        public async Task<IActionResult> SignupVerifyEmail([FromBody] SignupVerifyDTO dto, CancellationToken clt)
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

        [HttpPost("signup/resume")]
        [BlockIfAuthenticated]
        public async Task<IActionResult> SignupResumeSession([FromBody] SignupResumeDTO dto, CancellationToken clt)
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
                message = _localizer["Recovery_Success_SetNew"].Value
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
        [BlockIfAuthenticated] // TODO: determine if this should be blocked, unblocked, or make a new route for context of "recovery when still signed in"
        public async Task<IActionResult> SigninRequestRecovery(SigninRecoverDTO dto, CancellationToken clt)
        {
            ServiceResult<Unit> recoverResult = await _signinRequestRecoveryHandler.HandleAsync(dto.Email, clt);

            if(recoverResult.IsFailure && !_signupRecoverErrors.Contains(recoverResult.ErrorCode!.Value))
            {
                return StatusCode(500, new { message = _localizer["General_Error_500Server"].Value });
            }
            return Ok(new
            {
                message = _localizer["Recovery_Success_EmailSent"].Value, 
                provided_email = dto.Email
            });
        }

        [HttpPost("reset-password/verify")] 
        // [BlockIfAuthenticated] commented out to allow users who are already signed in to use password reset flow if they need to, can consider adding some extra checks in the handler later if needed
        public async Task<IActionResult> SigninConsumePasswordReset([FromBody] SigninResetPasswordDTO dto, CancellationToken clt)
        {
            ClientInfo clientInfo = new(HttpContext.Connection.RemoteIpAddress, HttpContext.Request.Headers.UserAgent.First());

            ServiceResult<CreatedSession> resetPasswordResult = await _signinVerifyPasswordResetHandler.HandleAsync(dto.Token, clientInfo, clt);

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
                message = _localizer["Recovery_Success_SetNew"].Value
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
                message = _localizer["Recovery_Success_PasswordReset"].Value
            });
        }

        [HttpPost("signout")]
        public async Task<IActionResult> Signout(CancellationToken clt)
        {
            // Guid sessionIdFromClaims = Guid.Parse(User.FindFirst("SessionId")!.Value);
            string? rawSessionId = Request.Cookies["sid"];

            if(string.IsNullOrEmpty(rawSessionId) || !Guid.TryParse(rawSessionId, out Guid sessionId)) 
            {
                await _signoutHandler.HandleAsync(Guid.CreateVersion7(), clt);
            }
            else {
                await _signoutHandler.HandleAsync(sessionId, clt);
            }

            HttpContext.Response.Cookies.Append(
                "sid",
                "",
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddDays(-1)
                }
            );

            return Ok(new 
            { 
                message = _localizer["Signout_Success"].Value
            });
        }
    }
}