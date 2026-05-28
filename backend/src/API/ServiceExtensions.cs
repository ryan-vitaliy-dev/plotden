using System.Reflection;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using Infrastructure.Auth;
using Microsoft.Extensions.Localization;
using Application.Resources;

namespace API
{
    public static class ApiServiceExtensions
    {
        public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddLocalization();

            services.AddRequestLocalization(options =>
                options.SetDefaultCulture("en")
                    .AddSupportedCultures(["en"])
            );

            services.Configure<PasswordHasherOptions>(options =>
                options.IterationCount = 200000
            );

            services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", builder =>
                {
                    builder.WithOrigins(configuration["Cors:AllowedOrigin"] ?? throw new InvalidOperationException("Cors:AllowedOrigin is not configured."))
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .AllowCredentials();
                });
            });

            services.AddAuthentication("SessionScheme")
                .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(
                    "SessionScheme", _ => {}
                );

            services.AddAuthorizationBuilder()
                .AddPolicy("ValidSession", policy => 
                    policy.RequireAuthenticatedUser()
                );

            services.AddControllers()
                .AddDataAnnotationsLocalization(options =>
                {
                    options.DataAnnotationLocalizerProvider = (type, factory) =>
                    {
                        var assemblyName = new AssemblyName(typeof(Application.Resources.SharedResource).GetType().Assembly.FullName!);
                        return factory.Create(nameof(Application.Resources.SharedResource), assemblyName.Name!);
                    };
                })
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.SuppressMapClientErrors = true;
                    options.InvalidModelStateResponseFactory = context =>
                    {
                        IStringLocalizer<SharedResource> localizer = context.HttpContext.RequestServices
                            .GetRequiredService<IStringLocalizer<SharedResource>>();

                        // TODO: Clean all this up later
                        string errorMessage;
                        var errors = context.ModelState
                            .SelectMany(kvp => kvp.Value!.Errors)
                            .Where(e => e.Exception == null)
                            .Select(e => e.ErrorMessage)
                        .ToList();

                        var firstError = context.ModelState.First(); // Always guaranteed to have one or more errors
                        string fieldName = firstError.Key;
                        if(fieldName.StartsWith('$') || fieldName.Contains("dto", StringComparison.OrdinalIgnoreCase))
                        {
                            errorMessage = localizer["General_Error_400BadRequest"].Value;
                        }
                        else
                        {
                            errorMessage = firstError.Value!.Errors.First().ErrorMessage;
                        }

                        return new BadRequestObjectResult(new { message = errorMessage });
                    };
                });

            return services;
        }

        public static WebApplication UseApiMiddleware(this WebApplication webApplication)
        {
            if(!webApplication.Environment.IsDevelopment())
            {
                webApplication.UseHttpsRedirection();
                webApplication.UseHsts();
            }
            // else
            // {
            //     // webApplication.MapOpenApi(); // Configure the HTTP request pipeline.
            // }
            webApplication.UseMiddleware<CustomErrorMiddleware>();
            webApplication.UseRequestLocalization();
            webApplication.UseCors("AllowFrontend");
            webApplication.UseAuthentication();
            webApplication.UseAuthorization();
            webApplication.MapControllers();

            return webApplication;
        }
    }
}