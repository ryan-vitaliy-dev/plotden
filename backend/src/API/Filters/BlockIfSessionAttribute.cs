
using Application.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Localization;

namespace API.Filters;

public class BlockIfAuthenticatedAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.HttpContext.Request.Cookies.ContainsKey("sid"))
        {
            var localizer = context.HttpContext.RequestServices
                .GetRequiredService<IStringLocalizer<SharedResource>>();

            context.Result = new BadRequestObjectResult(new { message = localizer["General_Error_AlreadyAuthenticated"].Value });
            return;
        }
        base.OnActionExecuting(context);
    }
}