using System.Text.Json;
using HotelOS.Formats;
using HotelOS.GuestOps.Application.Availability;
using HotelOS.Platform;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// The Setup screen's Stop-sell tab — <c>＋ Close a room type for dates</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>ADR 0377, owner 2026-10-04</b>: a stop-sell names a room type and may
/// additionally name one room within it. The type is required and the room is
/// not, so a body carrying a room and no type is refused by the shape of
/// <c>StopSellEdit</c> before any check runs — a room with no type is not
/// expressible, which is the ruling's own distinction from <i>type OR room</i>.
/// </para>
/// <para>
/// <b>Why this is a door and not a capability.</b> The table, the entity and
/// availability's subtraction have existed since the first migration; nothing
/// could create a row. One of the five Setup tabs was therefore readable and
/// unwritable, and the approved page draws its control live.
/// </para>
/// </remarks>
/// <param name="stopSells">Where the seller's control lives.</param>
public sealed class StopSellCommand(StopSellService stopSells)
{
    /// <summary>Close a type, or one of its rooms, for a range.</summary>
    /// <param name="scope">The caller, their property and their user.</param>
    /// <param name="body">The type, an optional room, the dates and the reason.</param>
    /// <param name="cancellationToken">Abandon the work.</param>
    /// <returns>What is now held, as the tab redraws from.</returns>
    /// <exception cref="InvalidRequestException">
    /// No body, no type, no dates, or a malformed date.
    /// </exception>
    public async Task<object?> RunAsync(
        RequestScope scope,
        JsonElement? body,
        CancellationToken cancellationToken)
    {
        if (body is not { ValueKind: JsonValueKind.Object } sheet)
        {
            throw new InvalidRequestException(
                "closing a room type needs the type, the dates and a reason");
        }

        var roomTypeId = Bodies.Id(sheet, "roomTypeId")
            ?? throw new InvalidRequestException(
                "a stop-sell needs the room type it is about — a room on its own does not "
                + "say what it is a room of");

        var edit = new StopSellEdit(
            roomTypeId,

            // Absent is the WHOLE TYPE, which is the ruling's own default and the
            // only reading under which `room type alone` means anything.
            Bodies.Id(sheet, "roomId"),
            Day(sheet, "fromDate"),
            Day(sheet, "toDate"),
            Text(sheet, "reason"));

        var row = await stopSells.SetAsync(scope, edit, cancellationToken);

        return new
        {
            stopSellId = row.Id.ToString(),
            roomTypeId = row.RoomTypeId.ToString(),

            // Echoed from what was STORED, and absent rather than empty where no
            // room was named: a screen reading "" would draw a hold on a room
            // whose id nobody sent.
            roomId = row.RoomId?.ToString(),
            fromDate = row.FromDate.ToString("O"),
            toDate = row.ToDate.ToString("O"),
        };
    }

    /// <summary>A date the caller must send, in the one format a wire carries.</summary>
    /// <remarks>
    /// <b>Through <c>Iso8601.Day</c>, which is the estate's one wire-date
    /// parser</b> — the same call <c>ModuleSurface.Date</c> and the gRPC door's
    /// <c>ParseDate</c> make. A `DateOnly.TryParse` here named
    /// <c>InvariantCulture</c> correctly and was still a SECOND implementation of
    /// a shared parser, which <c>WireParseGuardTests</c> caught: the guard's rule
    /// is about culture and its effect was to find a duplicate.
    /// </remarks>
    private static DateOnly Day(JsonElement body, string name)
        => body.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String
            && Iso8601.Day(value.GetString()) is { } day
                ? day
                : throw new InvalidRequestException(
                    $"a stop-sell needs {name} as an ISO-8601 date");

    private static string Text(JsonElement body, string name)
        => body.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : throw new InvalidRequestException($"a stop-sell needs {name}");
}
