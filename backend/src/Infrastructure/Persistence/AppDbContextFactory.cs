using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace backend.Infrastructure.Persistence
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        // NOTE: Used to let dotnet ef database update work with migrator user that has permissions to modify the tables
        public AppDbContext CreateDbContext(string[] args)
        {
            DbContextOptionsBuilder<AppDbContext> optionsBuilder = new();

            ConfigurationBuilder configurationBuilder = new();
            configurationBuilder.AddUserSecrets<AppDbContext>();
            configurationBuilder.AddEnvironmentVariables();
            IConfiguration configuration = configurationBuilder.Build();

            // You can replace this with your actual connection string or configuration
            optionsBuilder.UseNpgsql(configuration.GetConnectionString("Migrator"));

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}