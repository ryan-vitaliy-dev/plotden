using Microsoft.Extensions.DependencyInjection;

using Application.Accounts;
using Application.Auth;
using Application.Email;
using Application.Handlers.Signup;
using Application.Handlers.Signin;
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
            services.AddScoped<AccountService>();
            services.AddScoped<AuthService>();
            services.AddScoped<EmailService>();
            services.AddScoped<SessionService>();
            services.AddScoped<TokenService>();

            services.AddScoped<TokenEmailHandler>();

            services.AddScoped<UpdatePasswordHandler>();

            services.AddScoped<SigninRecoverHandler>();
            services.AddScoped<SigninHandler>();
            services.AddScoped<SigninResetPasswordHandler>();
            services.AddScoped<SigninApplyResetPasswordHandler>();

            services.AddScoped<SignupEmailHandler>();
            services.AddScoped<SignupVerifyHandler>();
            services.AddScoped<SignupResumeHandler>();
            services.AddScoped<SignupPasswordHandler>();

            return services;
        }
    }
}