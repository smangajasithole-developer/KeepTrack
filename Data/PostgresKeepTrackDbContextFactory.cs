
using keeptrack.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace keeptrack.Data;

public class PostgresKeepTrackDbContextFactory
    : IDesignTimeDbContextFactory<PostgresKeepTrackDbContext>
{
    public PostgresKeepTrackDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<PostgresKeepTrackDbContextFactory>()
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration
            .GetConnectionString("PostgresConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "PostgreSQL connection string was not found. " +
                "Check ConnectionStrings:PostgresConnection in User Secrets.");
        }

        var options = new DbContextOptionsBuilder<PostgresKeepTrackDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new PostgresKeepTrackDbContext(options);
    }
}
