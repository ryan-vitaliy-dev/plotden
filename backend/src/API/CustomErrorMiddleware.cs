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
                    503 => localizer["General_Error_503ServiceUnavailable"].Value,
                    500 => localizer["General_Error_500Server"].Value,
                    405 => localizer["General_Error_405MethodNotAllowed"].Value,
                    415 => localizer["General_Error_415UnsupportedMediaType"].Value,
                    403 => localizer["General_Error_403Forbidden"].Value,
                    _   => localizer["General_Error_400BadRequest"].Value
                };

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { message = errorMessage });
            }
        }
    }
}