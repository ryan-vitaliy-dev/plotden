using Application.Resources;
using Microsoft.Extensions.Localization;

namespace API
{
    public class CustomErrorMiddleware(RequestDelegate next, IStringLocalizer<SharedResource> localizer)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            await next(context);

            // Only intercept if no body has been written yet
            if (!context.Response.HasStarted && context.Response.StatusCode >= 400)
            {
                var errorMessage = context.Response.StatusCode switch
                {
                    405 => localizer["General_Error_405MethodNotAllowed"].Value,
                    415 => localizer["General_Error_415UnsupportedMediaType"].Value,
                    _   => localizer["General_Error_400BadRequest"].Value
                };

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { message = errorMessage });
            }
        }
    }
}