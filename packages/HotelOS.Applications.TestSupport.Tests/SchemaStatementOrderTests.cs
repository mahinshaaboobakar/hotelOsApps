using HotelOS.Applications.TestSupport;
using Xunit;

namespace HotelOS.Applications.TestSupport.Tests;

/// <summary>
/// The order step 4's statements come in, which is the part that cannot vary.
/// </summary>
/// <remarks>
/// <para>
/// A separate file from <see cref="ConventionNamesTests"/> because it answers a
/// different question and fails for a different reason: those say what things
/// are called, these say what has to happen before what. Both would be "the
/// convention's output" only in the sense that everything here is.
/// </para>
/// <para>
/// <b>The invariant is the installer's, not this class's.</b>
/// <c>services/kernel/crates/kernel/src/packages/database.rs:188-262</c> emits
/// these in one order: the owner must exist and be assumable before the schema
/// it authorises, the migrator must hold the owner before the application's own
/// <c>migrate</c> runs, and the provisioner hands back <c>SET</c> last so that
/// it keeps <c>ADMIN</c> — which is what <c>DROP ROLE</c> needs at uninstall.
/// An adoption that reordered them would still run, and would fail somewhere
/// else.
/// </para>
/// </remarks>
public sealed class SchemaStatementOrderTests
{
    private static List<string> Statements() =>
        new InstallerConvention("guestops", "abc123").SchemaStatements("scratch_db").ToList();

    /// <summary>The index of the first statement containing every fragment.</summary>
    private static int At(List<string> statements, params string[] fragments)
    {
        var found = statements.FindIndex(
            line => fragments.All(f => line.Contains(f, StringComparison.Ordinal)));

        Assert.True(found >= 0, $"no statement contains all of: {string.Join(" + ", fragments)}");
        return found;
    }

    [Fact]
    public void The_owner_is_assumable_before_the_schema_it_authorises()
    {
        var statements = Statements();

        // `CREATE SCHEMA … AUTHORIZATION` requires the creator be able to SET
        // ROLE to the owner, and PostgreSQL 16 grants a CREATEROLE role ADMIN
        // on what it creates but not SET.
        Assert.True(
            At(statements, "GRANT hotelos_owner_guestops_abc123 TO CURRENT_USER", "SET TRUE")
            < At(statements, "CREATE SCHEMA guestops AUTHORIZATION"));
    }

    [Fact]
    public void The_migrator_holds_the_owner_before_anything_could_migrate()
    {
        var statements = Statements();

        Assert.True(
            At(statements, "GRANT hotelos_owner_guestops_abc123 TO hotelos_migrator")
            < At(statements, "RESET ROLE"));
    }

    [Fact]
    public void Schema_scoped_grants_are_made_as_the_owner_and_the_role_is_reset()
    {
        var statements = Statements();

        var asOwner = At(statements, "SET ROLE hotelos_owner_guestops_abc123");
        var usage = At(statements, "GRANT USAGE ON SCHEMA guestops");
        var reset = At(statements, "RESET ROLE");

        // The provisioner created the schema and does not own it, so everything
        // schema-scoped is granted as the owner — and the session is handed back
        // before the cluster-level grants that follow.
        Assert.True(asOwner < usage);
        Assert.True(usage < reset);
    }

    [Fact]
    public void The_provisioner_gives_back_set_last_and_keeps_admin()
    {
        var statements = Statements();

        // Last, because ADMIN is what DROP ROLE needs at uninstall and SET is
        // what lets the provisioner *become* the owner. Giving SET back early
        // would break the grants above; not giving it back leaves a provisioner
        // able to act as an application's owner.
        Assert.Equal(
            statements.Count - 1,
            At(statements, "REVOKE SET OPTION FOR hotelos_owner_guestops_abc123"));
    }

    [Fact]
    public void The_application_gets_the_read_window_and_the_ability_to_announce()
    {
        var statements = Statements();

        // AUTHZ-Q23: the two grants an application holds on somebody else's
        // schema. A suite missing the second appends events that never publish.
        At(statements, "GRANT hotelos_masterdata_reader TO hotelos_app_guestops_abc123");
        At(statements, "GRANT hotelos_event_appender TO hotelos_app_guestops_abc123");
    }
}
