using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HotelOS.Workforce.Infrastructure;

/// <summary>
/// How <c>dotnet ef</c> builds the context when scaffolding a migration.
/// </summary>
/// <remarks>
/// <para>
/// <b>Design time only.</b> Nothing in the running application uses this — the
/// host registers the context in <c>Program.cs</c> — and the connection it opens
/// is a developer's, never a property's.
/// </para>
/// <para>
/// <b>Why an installable application always needs one.</b> Without a factory,
/// <c>dotnet ef</c> builds the application host to find the context. Master Data
/// survives that because an unenrolled machine puts it in bootstrap mode
/// (ADR 0069) and it starts anyway. <b>An installed application has no bootstrap
/// mode and must not have one</b>: it is installed *into* a property that
/// already exists, so its certificate exists before it does, and
/// <c>UsePlatformListener</c> refuses to start without one. On a developer's
/// machine that refusal is exactly what the tooling hits.
/// </para>
/// <para>
/// The alternative was to soften that guard so scaffolding could build the host.
/// <b>Rejected</b>, on the Knowledge service's precedent: an application that can
/// start without its enrolled identity is one listening on a cleartext loopback
/// socket, and §26 is explicit that loopback is not a trust boundary — the same
/// machine runs installed packages and connectors, any of which can open a socket
/// to 127.0.0.1. Trading a security property for a scaffolding convenience is the
/// wrong direction, and this factory costs nothing.
/// </para>
/// <para>
/// <b>It does not migrate anything.</b> Applying migrations is
/// <c>dotnet HotelOS.Workforce.dll migrate</c> — ADR 0039, and install step 6 —
/// which runs as the schema owner, so every object it creates is owned by this
/// application's owner role. Running <c>dotnet ef database update</c> through
/// this factory would leave every table owned by whichever role the connection
/// string names, and the per-schema default privileges would then silently grant
/// nothing.
/// </para>
/// </remarks>
public class WorkforceDesignTimeFactory : IDesignTimeDbContextFactory<WorkforceDbContext>
{
    /// <summary>The development database, unless the environment names another.</summary>
    /// <remarks>
    /// Port 25432 is the <b>development</b> cluster — ADR 0104. Never 15432,
    /// which is the installed product's: a scaffolding run pointed there would
    /// write a migration against a real property's database.
    /// </remarks>
    private const string DevelopmentConnection =
        "Host=127.0.0.1;Port=25432;Database=hotelos;Username=postgres;Password=devroot";

    /// <inheritdoc />
    public WorkforceDbContext CreateDbContext(string[] args)
    {
        var connection =
            Environment.GetEnvironmentVariable("ConnectionStrings__Workforce")
            ?? DevelopmentConnection;

        return new WorkforceDbContext(
            new DbContextOptionsBuilder<WorkforceDbContext>()
                // The same convention the runtime registration applies. Without
                // it the scaffolded migration would name columns in PascalCase
                // and disagree with the running application.
                .UseSnakeCaseNamingConvention()
                .UseNpgsql(
                    connection,
                    npgsql => npgsql.MigrationsHistoryTable(
                        "__migrations", WorkforceDbContext.Schema))
                .Options);
    }
}
