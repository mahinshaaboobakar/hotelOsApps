using HotelOS.Platform.TestSupport;
using Npgsql;

namespace HotelOS.Applications.TestSupport;

/// <summary>
/// The platform's own event store, on a scratch database.
/// </summary>
/// <remarks>
/// <para>
/// <b>Its own file because it is its own subject.</b> The model this was derived
/// from held it beside <see cref="InstallerConvention"/>, whose summary then
/// needed an "and" — <i>the installer's step 4, and the Kernel's event store</i>
/// — which ADR 0038 makes two files. They are also two owners, which is the
/// more useful reason: step 4 is the application-owned convention ADR 0157
/// moved here, and <c>event_store</c> is the Kernel's.
/// </para>
/// <para>
/// <b>What an installed application cannot provision and must not invent.</b>
/// <c>event_store</c> belongs to the Kernel: <c>03-schemas.sql</c> creates it
/// and the Kernel's own migrations fill it, and neither is something a package
/// runs. A wired suite nevertheless has to write events — a job that is raised
/// announces one in the same transaction — so this runs <b>the Kernel's own
/// migration text, read from the platform checkout</b>, rather than a table
/// shaped like it. A hand-written copy would be a second definition of the
/// platform's most shared table, and the suite would be proving an application
/// against a schema no property has.
/// </para>
/// <para>
/// <b>Where this sits against ADR 0157's boundary, stated rather than
/// assumed.</b> That ADR divides platform-owned infrastructure from
/// application-owned convention, and by that division <c>event_store</c> is the
/// platform's. This file satisfies the letter of all four constraints — it is
/// test-only, it creates no database, and it neither owns nor mutates the
/// canonical schema, because it <i>applies</i> the platform's own migrations
/// rather than restating them. But it is the one member here whose natural home
/// is arguably <c>HotelOS.Platform.TestSupport</c>, and moving it there is a
/// platform change rather than an application one. It lives here so that the
/// three applications share one copy instead of three; <b>that placement is
/// reported, not settled.</b>
/// </para>
/// </remarks>
public static class PlatformEventStore
{
    /// <summary>The role that may append, granted at install — <c>AUTHZ-Q23</c>.</summary>
    /// <remarks>
    /// The same role <see cref="InstallerConvention.EventAppender"/> names and
    /// <see cref="InstallerConvention.EnsureRolesAsync"/> creates. What it gains
    /// here is the schema to use it on.
    /// </remarks>
    private const string EventAppender = InstallerConvention.EventAppender;

    /// <summary>
    /// Create <c>event_store</c> from the Kernel's migrations, and grant it.
    /// </summary>
    /// <param name="databaseConnection">The provisioner's connection to the scratch database.</param>
    /// <returns>When the application role can append events.</returns>
    /// <exception cref="InvalidOperationException">The platform checkout was not found.</exception>
    public static async Task ProvisionAsync(string databaseConnection)
    {
        var migrations = Migrations();

        await using var connection = new NpgsqlConnection(databaseConnection);
        await connection.OpenAsync();

        await ExecuteAsync(connection, "CREATE SCHEMA IF NOT EXISTS event_store");

        // Ordered by name, which is what makes `0001` precede `0002`. The
        // Kernel applies them the same way and the numbering is the contract.
        foreach (var file in migrations.GetFiles("*.sql").OrderBy(f => f.Name, StringComparer.Ordinal))
        {
            await ExecuteAsync(connection, await File.ReadAllTextAsync(file.FullName));
        }

        foreach (var statement in new[]
        {
            $"GRANT USAGE ON SCHEMA event_store TO {EventAppender}",
            $"GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA event_store TO {EventAppender}",
        })
        {
            await ExecuteAsync(connection, statement);
        }
    }

    /// <summary>The Kernel's event-store migrations, found beside this checkout.</summary>
    /// <remarks>
    /// Walks up from the test assembly rather than taking a configured path:
    /// the platform is a sibling checkout, which is how every test project in
    /// this repository already reaches it, and a path that had to be configured
    /// would be a second place the layout is written down.
    /// </remarks>
    /// <returns>That directory.</returns>
    /// <exception cref="InvalidOperationException">It was not found.</exception>
    private static DirectoryInfo Migrations()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = new DirectoryInfo(Path.Combine(
                directory.FullName, "HosPilotOS", "services", "kernel", "crates", "kernel",
                "migrations", "event_store"));

            if (candidate.Exists)
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "the platform checkout was not found beside this one, so the Kernel's event-store "
            + "migrations cannot be read. A wired suite needs them: an application may not "
            + "invent event_store, and a suite that substituted a table shaped like it would be "
            + "proving itself against a schema no property has.");
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
