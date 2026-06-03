using System.Reflection;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using Scalar.AspNetCore;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;

using Application.Resources;
using Infrastructure.Auth;
using Domain.Sessions;

namespace API
{
    public static class ApiServiceExtensions
    {
        public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOpenApi();
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
                .AddPolicy("StandardSession", policy => 
                    policy.RequireAuthenticatedUser()
                    .RequireClaim("SessionType", SessionType.Standard.ToString())
                )
                .AddPolicy("IncompleteSignupSession", policy =>
                    policy.RequireAuthenticatedUser()
                    .RequireClaim("SessionType", SessionType.IncompleteSignup.ToString())
                )
                .AddPolicy("PasswordResetSession", policy =>
                    policy.RequireAuthenticatedUser()
                    .RequireClaim("SessionType", SessionType.PasswordReset.ToString())
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

                services.AddApiVersioning(options =>
                {
                    options.DefaultApiVersion = new ApiVersion(1);
                    options.AssumeDefaultVersionWhenUnspecified = true;
                    options.ReportApiVersions = true;
                    options.ApiVersionReader = new UrlSegmentApiVersionReader();
                }).AddApiExplorer(options =>
                {
                    options.GroupNameFormat = "'v'V";
                    options.SubstituteApiVersionInUrl = true;
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
            webApplication.UseMiddleware<CustomErrorMiddleware>();
            webApplication.UseRequestLocalization();
            webApplication.UseCors("AllowFrontend");
            webApplication.UseAuthentication();
            webApplication.UseAuthorization();
            webApplication.MapControllers();
            if(webApplication.Environment.IsDevelopment())
            {
                webApplication.MapOpenApi();
                webApplication.MapScalarApiReference();
            }

            return webApplication;
        }
    }
}