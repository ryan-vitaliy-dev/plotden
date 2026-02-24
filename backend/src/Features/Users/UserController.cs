using backend.Features.Users.DTOs;
using backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace backend.Features.Users
{

    [ApiController]
    [Route("api")]
    public class UserController(UserService userService, IStringLocalizerFactory factory) : ControllerBase
    {
        private readonly UserService _userService = userService;

        private readonly IStringLocalizer _userLocalizer = factory.Create(
            "Infrastructure.Localization.Resources.Features.User.ValidationMessages",
            typeof(Program).Assembly.GetName().Name!
        );

        
        [HttpPost("users")]
        public async Task<IActionResult> SignupUser([FromBody] UserSignupDTO userSignupDTO, CancellationToken clt)
        {
            return StatusCode(201, new { message = "Success."});
        }
    }
}