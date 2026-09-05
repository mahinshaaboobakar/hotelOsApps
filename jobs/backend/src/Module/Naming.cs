using HotelOS.Jobs.Application.Abstractions;
using HotelOS.Jobs.Domain;

namespace HotelOS.Jobs.Module;

/// <summary>
/// What the property calls a place and a person — and what to say when this
/// application cannot know.
/// </summary>
/// <remarks>
/// <para>
/// A screen draws "Room 1204" and "Arjun Menon". The job row holds a location
/// id and a user id, and only Master Data and Workforce can turn those into
/// words. Master Data is reachable, so a place is named. <b>Workforce is not</b>
/// — no client exists in the application SDK today — so a person is described
/// by what this service does know: that they are the guest of a stay, or a
/// member of staff, or a team.
/// </para>
/// <para>
/// <b>It says so rather than inventing.</b> A plausible name shown to a real
/// person is a fabricated identity, which the frame-beside-capture audit caught
/// once already in the header. The line a screen draws is therefore honest and
/// slightly poorer than the drawing until the Workforce read exists.
/// </para>
/// </remarks>
public sealed class Naming(IPropertyDirectory directory)
{
    private readonly Dictionary<Guid, string> places = [];

    /// <summary>What a person unspecified is drawn as — one em dash, everywhere.</summary>
    public const string Nobody = "—";

    /// <summary>The location's name, asked of Master Data once per request.</summary>
    public async Task<string> PlaceAsync(Guid propertyId, Guid locationId, CancellationToken cancellationToken)
    {
        if (places.TryGetValue(locationId, out var known)) return known;

        var name = await directory.FindLocationNameAsync(propertyId, locationId, cancellationToken);
        var answer = name ?? "not named here";
        places[locationId] = answer;
        return answer;
    }

    /// <summary>Who raised it, as far as this service can say.</summary>
    public static string Raiser(Job job) => job.RaisedKind switch
    {
        RaisedKind.Guest => job.StayId is { } stay ? $"Guest · stay {Short(stay)}" : "Guest",
        RaisedKind.Application => "Another application",
        _ => job.RaisedById is null ? Nobody : "Staff member",
    };

    /// <summary>Who holds it — a person, a team, awaiting AUTO, or nobody.</summary>
    public static string Assignee(JobAssignment? assignment) => assignment switch
    {
        null => Nobody,
        { TeamId: not null } => "A team",
        { AssigneeUserId: not null } => "Staff member",
        _ => "AUTO · pending",
    };

    /// <summary>The last four of an id — enough for a person to match two lines, never an identity.</summary>
    private static string Short(Guid id) => id.ToString("n")[^4..].ToUpperInvariant();
}
