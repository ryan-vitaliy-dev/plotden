using backend.Features.Accounts.DTOs;
using backend.Infrastructure.Common;
using backend.Infrastructure.Persistence;
using backend.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace backend.Features.Accounts
{

    [ApiController]
    [Route("api")]
    // IStringLocalizer<SharedResource> localizer
    public class AccountController(AccountService accountService, IStringLocalizer<SharedResource> localizer) : ControllerBase
    {
        private readonly AccountService _accountService = accountService;
        private readonly IStringLocalizer<SharedResource> _localizer = localizer;

        // private readonly IStringLocalizer _accountLocalizer = factory.Create(
        //     "Infrastructure.Localization.Resources.Features.Account.ValidationMessages",
        //     typeof(Program).Assembly.GetName().Name!
        // );

        [HttpGet("test")]
        public async Task<IActionResult> DoTest(CancellationToken clt)
        {
            ServiceResult<bool> res = await _accountService.TestAsync();
            if(res.IsSuccess)
            {
                return StatusCode(200, new { message = "Success." });
            }
            else
            {
                return StatusCode(400, new { message = "Failure." });
            }
        }
        
        [HttpPost("accounts")]
        public async Task<IActionResult> SignupAccount([FromBody] AccountSignupDTO accountSignupDTO, CancellationToken clt)
        {
            return StatusCode(201, new { message = "Success." });
            // "Success."
        }
    }
}