using System.Text.Json;
using HotelOS.GuestOps.Application.Bookings;
using HotelOS.GuestOps.Application.Stays;
using HotelOS.GuestOps.Domain;
using HotelOS.Platform;

namespace HotelOS.GuestOps.Module;

/// <summary>
/// Cancelling a booking — n cancellations of stays. Gold frame 8.
/// </summary>
/// <remarks>
/// <para>
/// <b>There is no such thing as cancelling a group.</b> A booking is a group
/// and every operation happens to a stay (GUEST-Q2, S23), so this loops and
/// records one cancellation per stay — which is also why either stay can be
/// reinstated on its own afterwards. The dialog says so before the button is
/// pressed; this is the same fact on the other side of it.
/// </para>
/// <para>
/// <b>A cancelled stay is not a deleted stay.</b> It keeps its row, its reason
/// and its penalty (S25, ADR 0062), and it stays in the bookings list.
/// </para>
/// <para>
/// <b>Nothing here reaches the PMS</b> (CONN-Q5, ADR 0128 §4). The dialog says
/// that out loud; this command has no outbound path at all, which is the
/// stronger form of the same statement — there is no call to forget to make.
/// </para>
/// </remarks>
public sealed class CancelCommand(
    BookingReadService bookings,
    StayLifecycleService lifecycle)
{
    /// <summary>Cancel every stay of a booking that can be cancelled.</summary>
    /// <param name="scope">The caller, and the property they are scoped to.</param>
    /// <param name="body">The booking, and the reason.</param>
    /// <param name="cancellationToken">The call's token.</param>
    /// <returns>What was cancelled, and what was left alone.</returns>
    public async Task<object?> RunAsync(
        RequestScope scope, JsonElement? body, CancellationToken cancellationToken)
    {
        if (body is not { ValueKind: JsonValueKind.Object } request)
        {
            throw new InvalidRequestException("a cancellation needs a booking");
        }

        var bookingId = Id(request, "bookingId")
            ?? throw new InvalidRequestException("a cancellation needs a booking");

        // **Required, and refused when absent.** A cancellation is reported on
        // and audited; a blank reason would make every one of them
        // indistinguishable afterwards. The screen offers the property's own
        // list, and where a property has configured none the desk types one.
        var reason = Text(request, "reason")
            ?? throw new InvalidRequestException("a cancellation needs a reason");

        var record = await bookings.GetAsync(scope, bookingId, cancellationToken);

        var cancelled = new List<string>();
        var untouched = new List<object>();

        foreach (var stay in record.Stays)
        {
            // A guest who has already arrived is corrected, not cancelled —
            // `StayLifecycleService` refuses it, and refusing here as well would
            // duplicate the rule. What this does is *not ask*: the stay is
            // reported back as left alone, so the desk sees which of a group's
            // rooms this did and did not touch.
            if (stay.Status is not (StayLifecycle.Booked or StayLifecycle.Waitlisted))
            {
                untouched.Add(new
                {
                    stayId = stay.Id.ToString(),
                    because = $"already {stay.Status.ToString().ToLowerInvariant()}",
                });

                continue;
            }

            // Read immediately before the write. The dialog's version would be
            // stale by the time a person confirms it — they read a plan, think
            // about it, and press the button a minute later, and in that minute
            // the PMS may have sent a fact about the same stay. Reading here
            // narrows the optimistic check to concurrent change rather than to
            // the operator's own reading time.
            var version = await bookings.StayVersionAsync(scope, stay.Id, cancellationToken);
            await lifecycle.CancelAsync(scope, stay.Id, reason, version, cancellationToken);
            cancelled.Add(stay.Id.ToString());
        }

        return new
        {
            bookingId = record.Id.ToString(),
            cancelled = cancelled.ToArray(),
            untouched = untouched.ToArray(),
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
