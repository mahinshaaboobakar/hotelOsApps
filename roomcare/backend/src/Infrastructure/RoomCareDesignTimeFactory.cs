using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HotelOS.RoomCare.Infrastructure;

/// <summary>The context <c>dotnet ef migrations add</c> builds — never used at runtime.</summary>
public class RoomCareDesignTimeFactory : IDesignTimeDbContextFactory<RoomCareDbContext>
{
    /// <summary>The development cluster; a migration is generated from the model, not from this database.</summary>
    private const string DevelopmentConnection =
        "Host=127.0.0.1;Port=25432;Database=hotelos;Username=hotelos_test;Password=devtest";

    public RoomCareDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__HotelOS") ?? DevelopmentConnection;

        return new RoomCareDbContext(
            new DbContextOptionsBuilder<RoomCareDbContext>()
                .UseSnakeCaseNamingConvention()
                .UseNpgsql(connection, npgsql => npgsql.MigrationsHistoryTable("__migrations", RoomCareDbContext.Schema))
                .Options);
    }
}
