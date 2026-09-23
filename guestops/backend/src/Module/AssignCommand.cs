using System.Text.Json;
using HotelOS.GuestOps.Application.Stays;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Infrastructure;
using HotelOS.Platform;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// Giving a stay a room, and moving it to another — gold frame 3's
/// <i>Move room</i>, and the day list's <c>＋ assign</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>The conflict warns and never forbids</b> — GUEST-Q5 made a double-booked
/// room a possible truth, so <c>StayAssignmentService</c> raises
/// <c>InUseException</c> on the first attempt and accepts the same call again
/// with <c>acceptConflict</c>. A hard block would put a ruled outcome out of
/// reach.
/// </para>
/// <para>
/// <b>So a conflict is part of the ANSWER, not an error from this command.</b>
/// It comes back as a refusal the screen can act on — naming the room and the
/// stay already holding it — rather than as a failure that would reach the desk
/// as <i>nothing was changed</i> with nothing to do about it. The exception's
/// own sentence is for the log: it is the platform's shared
/// <c>InUseException</c>, whose wording is about removal and does not fit an
/// assignment at all.
/// </para>
/// <para>
/// <b>The reason is derived, not sent.</b> A stay with no room is being given
/// its first — <c>Initial</c>; one that has a room is being moved —
/// <c>Move</c>. The service knows which, and the request has nowhere to put a
/// reason, so a client cannot record a move as a first assignment. <c>Upgrade</c>
/// and <c>Correction</c> are human judgements no screen in this application
/// makes yet, and inventing a field for them here would be a value nobody
/// chose.
/// </para>
/// </remarks>
/// <param name="db">Reading which room the stay has, to decide the reason.</param>
/// <param name="assignments">Where the assignment is actually made.</param>
public sealed class AssignCommand(GuestOpsDbContext db, StayAssignmentService assignments)
{
    /// <summary>Assign or move, and report a conflict rather than throwing one.</summary>
    /// <param name="scope">The caller, their property and their user.</param>
    /// <param name="body">The stay, the room, and the version it was read at.</param>
    /// <param name="cancellationToken">Abandon the work.</param>
    /// <returns>What happened, and what is in the way where something is.</returns>
    /// <exception cref="InvalidRequestException">A field neither can do without.</exception>
    public async Task<object?> RunAsync(
        RequestScope scope,
        JsonElement? body,
        CancellationToken cancellationToken)
    {
        if (body is not { ValueKind: JsonValueKind.Object } sheet)
        {
            throw new InvalidRequestException("an assignment needs a stay and a room");
        }

        var stayId = Id(sheet, "stayId")
            ?? throw new InvalidRequestException("an assignment needs the stay");

        var roomId = Id(sheet, "roomId")
            ?? throw new InvalidRequestException("an assignment needs the room");

        // **Refused rather than defaulted to zero.** A missing version read as
        // zero fails the concurrency check with a message about somebody else
        // having changed the stay — a claim about the world, when what happened
        // is that the caller never said which stay it read.
        var version = Version(sheet)
            ?? throw new InvalidRequestException(
                "an assignment needs the version the stay was read at");

        // Which room the stay has now decides whether this is a first
        // assignment or a move. Read here rather than accepted from the body.
        var held = await db.Stays
            .Where(stay => stay.Id == stayId && stay.PropertyId == scope.PropertyId)
            .Select(stay => stay.CurrentRoomId)
            .FirstOrDefaultAsync(cancellationToken);

        try
        {
            var stay = await assignments.AssignAsync(
                scope,
                stayId,
                roomId,
                held is null ? AssignmentReason.Initial : AssignmentReason.Move,

                // The desk sees the conflict before it accepts one: a first
                // attempt never accepts, and the screen asks again.
                acceptConflict: Accepted(sheet),
                version,
                cancellationToken);

            return new
            {
                assigned = true,
                version = stay.Version,
                roomId = stay.CurrentRoomId?.ToString(),
                moved = held is not null,
            };
        }
        catch (InUseException)
        {
            // Not an error to the desk — a question. The room is held by
            // another stay over these dates, and GUEST-Q5 says that can be
            // true on purpose.
            return new
            {
                assigned = false,
                conflict = true,
                version,
                moved = held is not null,
            };
        }
    }

    /// <summary>Whether the desk has seen the conflict and means it.</summary>
    /// <remarks>
    /// <b>Absent is not consent.</b> A body with no flag is a first attempt,
    /// which is what every screen sends before it has anything to show a person
    /// — the same rule the request log keeps for its hand-off.
    /// </remarks>
    private static bool Accepted(JsonElement body)
        => body.TryGetProperty("acceptConflict", out var value)
            && value.ValueKind == JsonValueKind.True;

    private static Guid? Id(JsonElement body, string name)
        => body.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String
            && Guid.TryParse(value.GetString(), out var id)
                ? id
                : null;

    private static long? Version(JsonElement body)
        => body.TryGetProperty("version", out var value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetInt64(out var version)
                ? version
                : null;
}
