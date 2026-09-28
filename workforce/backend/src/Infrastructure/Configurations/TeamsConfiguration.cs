using HotelOS.Workforce.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelOS.Workforce.Infrastructure.Configurations;

/// <summary>
/// How a team and its membership are stored — <c>Application/Teams</c>'s subject.
/// </summary>
/// <remarks>
/// Two types because a membership has no meaning without the team it is in, and
/// <c>TeamService</c> writes both in one transaction.
/// </remarks>
public sealed class TeamsConfiguration :
    IEntityTypeConfiguration<Team>,
    IEntityTypeConfiguration<TeamMember>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Team> team)
    {
        team.ToTable("teams", table =>
        {
            // A team is known by its name, so it has to have one. The same
            // check the department code carries, for the same reason: a row
            // that parsed as a string and holds nothing.
            table.HasCheckConstraint(
                "ck_teams__name_present",
                "length(btrim(name)) > 0");

            table.HasCheckConstraint(
                "ck_teams__department_code_present",
                "length(btrim(department_code)) > 0");
        });

        team.HasKey(t => t.Id);

        // The canon code's length is Master Data's `departments.code`.
        team.Property(t => t.DepartmentCode).HasMaxLength(50).IsRequired();
        team.Property(t => t.Name).HasMaxLength(200).IsRequired();
        team.Property(t => t.Version).IsConcurrencyToken();

        // Two live teams in one department may not share a name — a
        // supervisor picking "Team A" from two identical entries is choosing
        // at random. Enforced here as well as in the service, because the
        // service's check and a concurrent insert can both pass.
        team.HasIndex(t => new { t.PropertyId, t.DepartmentCode, t.Name })
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_teams__property_department_name");

        // The list every assignment screen draws.
        team.HasIndex(t => new { t.PropertyId, t.DepartmentCode })
            .HasDatabaseName("ix_teams__property_department");
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TeamMember> member)
    {
        member.ToTable("team_members", table =>
        {
            // A membership that ended before it began is a record that
            // cannot be true — WF-Q16's line, at the database.
            table.HasCheckConstraint(
                "ck_team_members__window_ordered",
                "left_on IS NULL OR left_on >= joined_on");
        });

        member.HasKey(m => m.Id);
        member.Property(m => m.Version).IsConcurrencyToken();

        // One live membership per person per team. A second would let a
        // removal close one row and leave the other standing, and the person
        // would still be in the team.
        member.HasIndex(m => new { m.TeamId, m.StaffId })
            .IsUnique()
            .HasFilter("left_on IS NULL")
            .HasDatabaseName("uq_team_members__team_staff_live");

        // Who is in this team, and which teams is this person in — the two
        // directions both get asked, the second when a posting ends.
        member.HasIndex(m => new { m.PropertyId, m.TeamId })
            .HasDatabaseName("ix_team_members__property_team");

        member.HasIndex(m => new { m.PropertyId, m.StaffId })
            .HasDatabaseName("ix_team_members__property_staff");
    }
}
