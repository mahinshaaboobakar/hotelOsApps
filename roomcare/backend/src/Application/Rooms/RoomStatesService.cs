using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Application.Days;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.RoomCare.Application.Rooms;

/// <summary>The Room states tab's one Save — many rooms' four facts, each a manual observation (redlines 4–5).</summary>
/// <remarks>
/// <para>
/// Available at every property and never gated by where stay facts usually
/// come from; it rides <c>roomcare.amend</c>. Each edited room is written as a
/// <c>room_observation</c> with source <c>MANUAL</c> — a deliberate act for the
/// ordering clause — so a later, older PMS message cannot undo it.
/// </para>
/// <para>
/// <b>A row the PMS has updated since the screen was drawn is not saved.</b>
/// The screen sends the version it showed; a moved version comes back as a
/// conflict row for the person to look at, and the rest of the batch saves.
/// One conflict never loses forty good edits.
/// </para>
/// </remarks>
public sealed class RoomStatesService(RoomCareDbContext db, Gate gate, ObservationService observations, PropertyClock clock)
{
    /// <summary>The most rooms one Save may carry — the whole of a large house, with room to spare.</summary>
    public const int MaxRooms = 600;

    public async Task<RoomStatesSaved> SaveAsync(
        RequestScope scope, IReadOnlyList<RoomStateEdit> edits, CancellationToken cancellationToken)
    {
        if (edits.Count == 0 || edits.Count > MaxRooms)
        {
            throw new InvalidRequestException($"a save carries between 1 and {MaxRooms} rooms");
        }

        var person = Actor.PersonOf(scope, "setting a room's state");
        var actor = Actor.Of(scope);
        var now = await clock.AtAsync(scope.PropertyId, cancellationToken);
        var ids = edits.Select(e => e.RoomId).ToList();
        var current = await db.RoomStates
            .Where(r => r.PropertyId == scope.PropertyId && ids.Contains(r.RoomId))
            .ToDictionaryAsync(r => r.RoomId, cancellationToken);

        var conflicts = new List<RoomStateConflict>();
        var saved = 0;
        foreach (var edit in edits)
        {
            Validate(edit);
            if (current.TryGetValue(edit.RoomId, out var room) && room.Version != edit.ExpectedVersion)
            {
                conflicts.Add(new RoomStateConflict(edit.RoomId, room.Version, room.ConditionSource, room.LastObservedAt));
                continue;
            }

            await gate.RoomAsync(scope, Permissions.Amend, edit.RoomId, cancellationToken);
            await observations.ObserveAsync(scope, new ObservedFact(edit.RoomId, ObservationSource.Manual, now.Instant, now.Instant)
            {
                OperatingDay = now.Day,
                Condition = edit.Condition,
                Occupancy = edit.Occupancy ?? OccupancyOf(edit.Stay),
                StayStatuses = StatusesOf(edit.Stay),
                // "Arrival expected today 15:00" sets the sale at that local time;
                // clearing it says the room is not sold tonight.
                SaysNextSold = edit.SoldAt is not null || edit.ClearSold,
                NextSoldAt = edit.SoldAt is { } time ? now.InstantOf(now.LocalDate, time) : null,
                ByUserId = person,
                Via = actor.Via,
            }, cancellationToken);
            saved++;
        }

        await db.SaveChangesAsync(cancellationToken);
        return new RoomStatesSaved(saved, conflicts);
    }

    private static void Validate(RoomStateEdit edit)
    {
        if (edit.Condition is { } c && !Condition.All.Contains(c))
        {
            throw new InvalidRequestException($"'{c}' is not a condition a room can be set to here");
        }

        if (edit.Occupancy is { } o && !Occupancy.All.Contains(o))
        {
            throw new InvalidRequestException($"'{o}' is not an occupancy");
        }

        if (edit.Stay is { } stay && !StayWords.Contains(stay))
        {
            throw new InvalidRequestException($"'{stay}' is not a stay — departed, arrived, in house or none");
        }
    }

    /// <summary>The stay words the tab paints with (frame 4d): departed · arrived · in house · none.</summary>
    public static readonly IReadOnlyList<string> StayWords = ["DEPARTED", "ARRIVED", "IN_HOUSE", "NONE"];

    private static IReadOnlyList<string>? StatusesOf(string? stay) => stay switch
    {
        "DEPARTED" => [StayStatus.CheckedOut],
        "ARRIVED" or "IN_HOUSE" => [StayStatus.CheckedIn],
        "NONE" => [],
        _ => null,
    };

    private static string? OccupancyOf(string? stay) => stay switch
    {
        "DEPARTED" or "NONE" => Occupancy.Vacant,
        "ARRIVED" or "IN_HOUSE" => Occupancy.Occupied,
        _ => null,
    };
}

/// <summary>One room's edit from the Room states tab; null means "not changed".</summary>
public sealed record RoomStateEdit(Guid RoomId, long ExpectedVersion)
{
    public string? Condition { get; init; }

    public string? Occupancy { get; init; }

    /// <summary>The expected arrival today, property-local — "sets sold tonight".</summary>
    public TimeOnly? SoldAt { get; init; }

    /// <summary>The room is not sold tonight after all.</summary>
    public bool ClearSold { get; init; }

    /// <summary>The stay word; the occupancy follows it unless given.</summary>
    public string? Stay { get; init; }
}

/// <summary>A room not saved because something else changed it after the screen was drawn.</summary>
public sealed record RoomStateConflict(Guid RoomId, long Version, string Source, DateTimeOffset? LastObservedAt);

/// <summary>How many rooms saved, and which came back to look at.</summary>
public sealed record RoomStatesSaved(int Saved, IReadOnlyList<RoomStateConflict> Conflicts);
