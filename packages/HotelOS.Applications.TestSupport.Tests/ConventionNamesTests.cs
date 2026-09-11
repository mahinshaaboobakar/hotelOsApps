using HotelOS.Applications.TestSupport;
using Xunit;

namespace HotelOS.Applications.TestSupport.Tests;

/// <summary>
/// What the convention calls things, and what it refuses to call anything.
/// </summary>
/// <remarks>
/// The names are spelled out here rather than imported from the class under
/// test: an assertion against a constant the code also imports agrees with
/// itself whatever either says.
/// </remarks>
public sealed class ConventionNamesTests
{
    /// <summary>A run id in the shape the application fixtures actually form.</summary>
    /// <remarks>
    /// <c>Guid.NewGuid().ToString("n")[..8]</c> — Jobs' fixture, verbatim. It is
    /// taken from the caller rather than invented here because the value's
    /// *shape* is the thing under test.
    /// </remarks>
    private static string Run() => Guid.NewGuid().ToString("n")[..8];

    [Fact]
    public void Names_are_the_installers_with_this_runs_suffix()
    {
        var convention = new InstallerConvention("guestops", "abc123");

        Assert.Equal("guestops", convention.Schema);
        Assert.Equal("hotelos_owner_guestops_abc123", convention.OwnerRole);
        Assert.Equal("hotelos_app_guestops_abc123", convention.AppRole);
    }

    [Fact]
    public void Every_application_gets_its_own_pair()
    {
        // The parameter earning its keep: one class, three applications, and no
        // two of them naming the same cluster role.
        var names = new[] { "guestops", "jobs", "workforce" }
            .Select(schema => new InstallerConvention(schema, "abc123"))
            .SelectMany(one => new[] { one.OwnerRole, one.AppRole })
            .ToList();

        Assert.Equal(names.Count, names.Distinct().Count());
    }

    [Fact]
    public void A_real_run_id_is_accepted_whatever_character_it_starts_with()
    {
        // **The defect this test exists for, and it was mine.** The first
        // version validated the run id with the same rule as the schema, which
        // requires a leading letter. A run id is eight hex characters and six
        // of the sixteen possible first characters are digits — so shared code
        // two applications were about to adopt would have thrown on roughly
        // three runs in eight, at random.
        //
        // A hand-picked fixture value would have passed forever. This generates
        // the shape the fixtures generate, and asserts the digit-leading case
        // explicitly so the coverage does not depend on luck.
        for (var i = 0; i < 200; i++)
        {
            var run = Run();
            var convention = new InstallerConvention("guestops", run);

            Assert.Equal($"hotelos_app_guestops_{run}", convention.AppRole);
        }

        Assert.Equal(
            "hotelos_app_guestops_0f3a9c21",
            new InstallerConvention("guestops", "0f3a9c21").AppRole);
    }

    [Theory]
    [InlineData("guest ops")]
    [InlineData("guestops; DROP SCHEMA public")]
    [InlineData("GuestOps")]
    [InlineData("1guestops")]
    [InlineData("")]
    public void A_schema_that_could_not_be_installed_is_refused(string schema)
    {
        // Every name here reaches SQL by interpolation, because an identifier
        // cannot be a parameter. The platform refuses the same shapes at install
        // as UnsafeSchemaName, and a fixture that accepted one would be standing
        // somewhere no property can stand.
        Assert.Throws<ArgumentException>(() => new InstallerConvention(schema, "abc123"));
    }

    [Theory]
    [InlineData("abc 123")]
    [InlineData("abc'123")]
    [InlineData("")]
    public void A_run_id_that_could_not_be_part_of_a_name_is_refused(string run)
    {
        Assert.Throws<ArgumentException>(() => new InstallerConvention("guestops", run));
    }
}
