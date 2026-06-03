using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Localization;

using Application.Common;
using Application.Resources;
using Application.Sessions;
using Domain.Common;

namespace API.Filters;

public class BlockIfAuthenticatedAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.Request.Cookies.TryGetValue("sid", out string? sid))
        {
            CancellationToken clt = context.HttpContext.RequestAborted; 

            bool parse = Guid.TryParse(sid, out Guid sessionId);
            if(!parse) {
                await next();
            }

            // TODO: Eventually replace this with ISessionService maybe
            SessionService sessionService = context.HttpContext.RequestServices
                .GetRequiredService<SessionService>();

            ServiceResult<Unit> isSessionValidResult = await sessionService.IsSessionValidAsync(sessionId, clt);
            if(isSessionValidResult.IsFailure)
            {
                // session is not valid anyways, so just continue
                await next();
            }
            // valid session, reject

            IStringLocalizer<SharedResource> localizer = context.HttpContext.RequestServices
                .GetRequiredService<IStringLocalizer<SharedResource>>();

            // Short-circuits the pipeline and sends result immediately
            context.Result = new BadRequestObjectResult(new { message = localizer["General_Error_AlreadyAuthenticated"].Value }); 
            return;
        }
        await next();
    }
}