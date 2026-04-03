using System.Net;

// using backend.Application.Accounts.DTOs;
using Application.Handlers;
using Application.Handlers.Signup;
using Domain.Accounts;


// using backend.Features.Auth.Handlers;
using Infrastructure.Common;
using Infrastructure.Persistence;
using Application.Resources;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace API.Controllers.Accounts
{

    [ApiController]
    [Route("api/accounts")]
    // IStringLocalizer<SharedResource> localizer
    public class AccountController(
        SignupEmailHandler signupEmailHandler,
        SignupVerifyHandler signupVerifyHandler,
        IStringLocalizer<SharedResource> localizer
        ) : ControllerBase
    {
        private readonly SignupEmailHandler _signupEmailHandler = signupEmailHandler;

        private readonly SignupVerifyHandler _signupVerifyHandler = signupVerifyHandler;
        private readonly IStringLocalizer<SharedResource> _localizer = localizer;

        //[HttpPatch("signup/password")] - handles setting password on signup
        //[HttpPatch("settings/email")] - handles updating email
        //[HttpPatch("settings/password")] - handles updating password later

        //[HttpPatch("profiles/username")] - handles updating username
    }
}