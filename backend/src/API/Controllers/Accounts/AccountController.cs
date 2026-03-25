using backend.Application.Accounts.DTOs;
using backend.Application.Accounts.Handlers;
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
    public class AccountController(SignupAccountHandler signupHandler, IStringLocalizer<SharedResource> localizer) : ControllerBase
    {
        private readonly SignupAccountHandler _signupHandler = signupHandler;
        private readonly IStringLocalizer<SharedResource> _localizer = localizer;

        // private readonly IStringLocalizer _accountLocalizer = factory.Create(
        //     "Infrastructure.Localization.Resources.Features.Account.ValidationMessages",
        //     typeof(Program).Assembly.GetName().Name!
        // );

        // [HttpGet("test")]
        // public async Task<IActionResult> DoTest(CancellationToken clt)
        // {
        //     ServiceResult<bool> res = await _accountService.TestAsync(clt);
        //     if(res.IsSuccess)
        //     {
        //         return StatusCode(200, new { message = "Success." });
        //     }
        //     else
        //     {
        //         return StatusCode(400, new { message = "Failure." });
        //     }
        // }
        
        [HttpPost("accounts")]
        public async Task<IActionResult> SignupAccount([FromBody] AccountSignupDTO dto, CancellationToken clt)
        {

            ServiceResult<Account> signupResult = await _signupHandler.HandleAsync(dto, clt);
            
            if(signupResult.IsFailure)
            {
                return signupResult.ErrorCode switch
                {
                    ServiceError.InvalidInput => BadRequest(new { message = _localizer["GeneralBadRequest"]}),
                    ServiceError.OperationCancelled => StatusCode(499),
                    _ => StatusCode(500, new { message = _localizer["GeneralServerError"]})
                };
            }

            Account newAccount = signupResult.Value;
            return StatusCode(201, new { 
                message = "Signed up account.", 
                email = newAccount.Email
            });
        }
    }
}