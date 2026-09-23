using System.Text.Json;
using HotelOS.GuestOps.Application.Requests;
using HotelOS.Platform;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// Logging a guest's request, and adding a note — gold frame 5, under
/// <c>request.handle</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>`StayRequestService` has recorded requests since it was written and no
/// screen could reach it.</b> The Requests tab drew *＋ Log a request* and the
/// control did nothing, which the capability ledger has carried as a defect
/// since 2026-09-19. This is the door.
/// </para>
/// <para>
/// <b>A request is GuestOps' own record, whether or not Jobs is installed</b> —
/// gold frame 5b, and the rule behind every dimmed panel on those screens: *"an
/// installable platform means the property that has not bought Jobs still has a
/// guest complaining about the air conditioning"*. So nothing here asks whether
/// Jobs exists. `handOff` says the desk wants work raised from it; the request
/// is stored either way, and a property with no Jobs simply never sets it.
/// </para>
/// <para>
/// <b>Recording and handing off are one call</b>, which is the service's own
/// reasoning and is why this door does not offer a second method: a request
/// recorded and then handed off in a later step can be forgotten in the gap,
/// which is the failure the paper log had.
/// </para>
/// </remarks>
/// <param name="requests">Records the request or the note.</param>
public sealed class RequestCommand(StayRequestService requests)
{
    /// <summary>Log a request against a stay, or add a note to it.</summary>
    /// <param name="scope">The caller, their property and their user.</param>
    /// <param name="method">Which of the two the screen asked for.</param>
    /// <param name="body">The stay, the text, and whether work is wanted.</param>
    /// <param name="cancellationToken">Abandon the work.</param>
    /// <returns>What was recorded, as the tab reads it back.</returns>
    /// <exception cref="InvalidRequestException">A field neither can do without.</exception>
    public async Task<object?> RunAsync(
        string method,
        RequestScope scope,
        JsonElement? body,
        CancellationToken cancellationToken)
    {
        if (body is not { ValueKind: JsonValueKind.Object } sheet)
        {
            throw new InvalidRequestException("a request needs a stay and what was asked for");
        }

        var stayId = Id(sheet, "stayId")
            ?? throw new InvalidRequestException("a request needs the stay it is about");

        var text = Text(sheet, "text")
            ?? throw new InvalidRequestException("a request needs what the guest asked for");

        if (method == "note")
        {
            var note = await requests.AddNoteAsync(scope, stayId, text, cancellationToken);

            return new { noteId = note.Id.ToString(), at = note.At.ToString("O") };
        }

        var logged = await requests.LogAsync(
            scope,
            stayId,
            text,

            // **Absent is not consent.** A screen that wants work raised says
            // so; a body with no flag records the request and asks for nothing,
            // which is what a property without Jobs sends every time.
            sheet.TryGetProperty("handOff", out var wanted) && wanted.ValueKind == JsonValueKind.True,
            cancellationToken);

        return new
        {
            requestId = logged.Id.ToString(),
            at = logged.LoggedAt.ToString("O"),

            // What the desk asked for, echoed from what was STORED rather than
            // from what was sent: the tab draws "raised as …" from this, and a
            // screen that drew its own request back would be reporting its own
            // intention as a fact.
            handedOff = logged.HandedOff,
        };
    }

    private static string? Text(JsonElement body, string name)
        => body.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(value.GetString())
                ? value.GetString()
                : null;

    private static Guid? Id(JsonElement body, string name)
        => Text(body, name) is { } text && Guid.TryParse(text, out var id) ? id : null;
}
