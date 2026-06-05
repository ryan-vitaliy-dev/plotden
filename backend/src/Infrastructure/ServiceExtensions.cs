using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Application.Common.Interfaces;
using Infrastructure.Common;
using Infrastructure.Email;
using Infrastructure.Persistence;
using Infrastructure.Security;

namespace Infrastructure
{
    public static class InfrastructureServiceExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                config.GetConnectionString("AppDb"),
                b => b.MigrationsAssembly("Infrastructure")
            ));
            // .LogTo(Console.WriteLine, LogLevel.Information));

            services.AddScoped<IAppDbContext, AppDbContext>();

            services.AddTransient<IUsernameGenerator, AdjectiveNounUsernameGenerator>();
            services.AddTransient<ITokenGenerator, SecureTokenGenerator>();

            services.AddTransient<IEmailSender, FluentEmailSender>();
            services.AddScoped<IEmailTemplateLoader, EmailTemplateLoader>();

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}