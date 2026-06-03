using Microsoft.Extensions.DependencyInjection;

using Application.Accounts;
using Application.Auth;
using Application.Email;
using Application.Handlers.Auth;
using Application.Sessions;
using Application.Tokens;
using Application.Handlers.Common;
using Application.Handlers.Accounts;

namespace Application
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<SessionService>();
            services.AddScoped<TokenService>();
            services.AddScoped<EmailService>();
            services.AddScoped<AuthService>();
            services.AddScoped<AccountService>();

            services.AddScoped<TokenEmailHandler>();

            services.AddScoped<UpdatePasswordHandler>();

            services.AddScoped<SigninHandler>();
            services.AddScoped<SigninRequestRecoveryHandler>();
            services.AddScoped<SigninConsumePasswordResetHandler>();
            services.AddScoped<SigninSetPasswordResetHandler>();

            services.AddScoped<SignupRequestEmailHandler>();
            services.AddScoped<SignupVerifyEmailHandler>();
            services.AddScoped<SignupResumeSessionHandler>();
            services.AddScoped<SignupSetPasswordHandler>();

            return services;
        }
    }
}