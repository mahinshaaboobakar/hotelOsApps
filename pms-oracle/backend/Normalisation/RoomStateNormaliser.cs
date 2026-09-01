using HotelOS.Contracts.Common.V1;
using System.Globalization;
using Google.Protobuf.WellKnownTypes;
using HotelOS.Contracts.Integration.V1;
using PmsOracle.Integrations.OnSite;
using PmsOracle.Vocabularies;

namespace PmsOracle.Normalisation;

/// <summary>
/// Turns one on-site room-status message into a normalised room-state fact.
/// </summary>
/// <remarks>
/// <para>
/// The four axes stay four. Occupancy and condition are read through separate
/// vocabularies into separate fields, the stays touching the room become a
/// list, and an axis the source did not send is left <b>absent rather than
/// defaulted</b> — a room-state screen shows what the PMS said, not what we
/// assumed (R1).
/// </para>
/// <para>
/// <c>NextBlocked</c> is carried because it is the field that separates two
/// rooms identical on all four axes: occupied, dirty and due out, one sold
/// again tonight and one not (R3).
/// </para>
/// </remarks>
public sealed class RoomStateNormaliser
{
    /// <summary>The next-blocked date's format — a third one in this integration (R15).</summary>
    private const string NextBlockedFormat = "dd-MM-yy";

    private readonly IntegrationSettings _settings;

    /// <summary>Construct for one configured integration.</summary>
    /// <param name="settings">That integration's identity and property configuration.</param>
    public RoomStateNormaliser(IntegrationSettings settings) => _settings = settings;

    /// <summary>Normalise one room-status message.</summary>
    /// <param name="push">The message as it arrived.</param>
    /// <returns>A fact, or a rejection naming what it could not use.</returns>
    public NormalisationOutcome Normalise(OnSiteRoomStatusPush push)
    {
        if (!string.IsNullOrWhiteSpace(push.PropertyCode)
            && !string.Equals(push.PropertyCode, _settings.PropertyCode, StringComparison.Ordinal))
        {
            return Reject(RejectionReason.PropertyMismatch, "PropertyCode", push.PropertyCode);
        }

        if (string.IsNullOrWhiteSpace(push.RoomNo))
        {
            // Without a room there is nothing this message is about. The
            // reference recorded the same condition as `ROOM_NO_MISSING`.
            return Reject(RejectionReason.MissingRequiredField, "RoomNo", push.RoomNo);
        }

        var state = new RoomState();

        state.ExternalRefs.Add(new ExternalRef
        {
            IntegrationId = _settings.IntegrationId,
            IdentifierKind = RoomNumberKind,
            ExternalId = push.RoomNo,
        });

        if (!string.IsNullOrWhiteSpace(push.FOStatus))
        {
            var occupancy = FrontOfficeCodes.Read(push.FOStatus);
            if (!occupancy.TryGet(out var value))
            {
                return Reject(RejectionReason.UnknownStatus, "FOStatus", occupancy.UnrecognisedValue);
            }

            state.Occupancy = value;
        }

        if (!string.IsNullOrWhiteSpace(push.RoomStatus))
        {
            var condition = RoomConditionCodes.ReadOnSite(push.RoomStatus);
            if (!condition.TryGet(out var value))
            {
                return Reject(RejectionReason.UnknownStatus, "RoomStatus", condition.UnrecognisedValue);
            }

            state.Condition = value;
        }

        // The housekeeping department's own axis: the on-site flavours do not
        // send it, so it stays UNSPECIFIED rather than being filled from the
        // condition above. Two axes that a source happens not to distinguish
        // are still two axes.

        var stays = ReadStays(push.ReservationStatus);
        if (stays.Rejected is not null)
        {
            return stays.Rejected;
        }

        state.StayStatuses.Add(stays.Lifecycles);

        var nextSold = ReadNextBlocked(push.NextBlocked);
        if (nextSold is not null)
        {
            state.NextSoldAt = Timestamp.FromDateTimeOffset(nextSold.Value);
        }

        return new NormalisationOutcome.RoomStateNormalised(new RoomStateFact
        {
            Header = new FactHeader { PropertyId = _settings.PropertyId },
            State = state,
        });
    }

    /// <summary>The identifier kind for an on-site room number.</summary>
    /// <remarks>
    /// Connector-declared, per <c>CONN-Q8</c>. A room is a <b>master</b> entity,
    /// so unlike a reservation's reference this one is resolved by Enrich onto
    /// <c>masterdata.room_id</c> — <c>GUEST-Q8(a)</c>'s split — and the fact is
    /// held unmappable until it can be.
    /// </remarks>
    public const string RoomNumberKind = "room-number";

    /// <summary>
    /// Read the comma-separated stay list — <b>all of it</b>.
    /// </summary>
    /// <remarks>
    /// The reference split this string and took element zero. A room with a
    /// departure and an arrival on the same day then had one status, and the
    /// other stay was not merely unreported: it was invisible.
    /// </remarks>
    private (List<StayLifecycle> Lifecycles, NormalisationOutcome? Rejected) ReadStays(string? sourceValue)
    {
        var lifecycles = new List<StayLifecycle>();

        if (string.IsNullOrWhiteSpace(sourceValue))
        {
            return (lifecycles, null);
        }

        foreach (var part in sourceValue.Split(',', StringSplitOptions.TrimEntries))
        {
            if (part.Length == 0)
            {
                continue;
            }

            var reading = RoomStayStatusCodes.Read(part, out var contributesStay);
            if (!reading.TryGet(out var lifecycle))
            {
                return (lifecycles,
                    Reject(RejectionReason.UnknownStatus, "ReservationStatus", reading.UnrecognisedValue));
            }

            if (contributesStay)
            {
                lifecycles.Add(lifecycle);
            }
        }

        return (lifecycles, null);
    }

    /// <summary>
    /// Read the next-blocked date into a moment in the property's zone.
    /// </summary>
    /// <remarks>
    /// A date with no time, so it is completed with the property's check-in
    /// time: "next sold" means next arrival, and an arrival happens at check-in
    /// (R12). Two-digit years are read through the invariant calendar's
    /// windowing rather than a rule of our own.
    /// </remarks>
    private DateTimeOffset? ReadNextBlocked(string? sourceValue) =>
        !string.IsNullOrWhiteSpace(sourceValue)
        && DateOnly.TryParseExact(
            sourceValue, NextBlockedFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? _settings.Clock.ArrivalOn(date)
            : null;

    private static NormalisationOutcome Reject(RejectionReason reason, string field, string? raw) =>
        new NormalisationOutcome.Rejected(reason, field, raw);
}
