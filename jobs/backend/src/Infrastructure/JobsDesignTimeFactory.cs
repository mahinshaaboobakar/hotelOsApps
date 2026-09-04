using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HotelOS.Jobs.Infrastructure;

/// <summary>How <c>dotnet ef migrations add</c> builds the context — the development database, or the connection string the environment names.</summary>
public class JobsDesignTimeFactory : IDesignTimeDbContextFactory<JobsDbContext>
{
    private const string DevelopmentConnection =
        "Host=127.0.0.1;Port=25432;Database=hotelos;Username=postgres;Password=devroot";

    public JobsDbContext CreateDbContext(string[] args)
    {
        var connection =
            Environment.GetEnvironmentVariable("ConnectionStrings__Jobs")
            ?? DevelopmentConnection;

        return new JobsDbContext(
            new DbContextOptionsBuilder<JobsDbContext>()
                .UseSnakeCaseNamingConvention()
                .UseNpgsql(
                    connection,
                    npgsql => npgsql.MigrationsHistoryTable("__migrations", JobsDbContext.Schema))
                .Options);
    }
}
