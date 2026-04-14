using System.Reflection;
using Application.Accounts;
using Application.Auth;
using Application.Email;
using Application.Handlers.Signup;
using Application.Sessions;
using Application.Tokens;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Application
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<AccountService>();
            services.AddScoped<AuthService>();
            services.AddScoped<EmailService>();
            services.AddScoped<SessionService>();
            services.AddScoped<TokenService>();

            services.AddScoped<SignupEmailHandler>();
            services.AddScoped<SignupVerifyHandler>();
            services.AddScoped<SignupPasswordHandler>();

            return services;
        }
    }
}