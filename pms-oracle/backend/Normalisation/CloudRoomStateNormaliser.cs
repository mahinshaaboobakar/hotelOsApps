using HotelOS.Contracts.Integration.V1;
using PmsOracle.Integrations.Cloud;
using PmsOracle.Vocabularies;

namespace PmsOracle.Normalisation;

/// <summary>
/// Turns one room from OHIP's housekeeping overview into a normalised
/// room-state fact.
/// </summary>
/// <remarks>
/// <para>
/// The same four axes as the on-site flavour's push, in OHIP's own words, plus
/// two things only this source supplies: the housekeeping department's own
/// status, and the pseudo-room flag (R4).
/// </para>
/// <para>
/// <b>Absent and empty are different here, and the difference is the point.</b>
/// A null status is an axis OHIP did not send and stays
/// <c>UNSPECIFIED</c>; an <b>empty string</b> is OHIP saying the room needs a
/// pick-up, which is a condition the floor works from (R5). Reading blank as
/// unknown is the loss this connector exists not to repeat.
/// </para>
/// </remarks>
public sealed class CloudRoomStateNormaliser
{
    private readonly IntegrationSettings _settings;

    /// <summary>Construct for one configured integration.</summary>
    /// <param name="settings">That integration's identity and property configuration.</param>
    public CloudRoomStateNormaliser(IntegrationSettings settings) => _settings = settings;

    /// <summary>Normalise one room from the housekeeping overview.</summary>
    /// <param name="room">The room as OHIP returned it.</param>
    /// <returns>A fact, or a rejection naming what it could not use.</returns>
    public NormalisationOutcome Normalise(OhipHousekeepingRoom room)
    {
        if (string.IsNullOrWhiteSpace(room.RoomId))
        {
            return Reject(RejectionReason.MissingRequiredField, "roomId", room.RoomId);
        }

        var state = new RoomState
        {
            // R4. Carried rather than filtered: an operator marking a house
            // account as "not a physical room" is the unmappable queue doing
            // its job, and a connector that dropped these would leave that
            // person wondering why some rooms never arrive.
            IsPseudoRoom = room.RoomType?.PseudoRoom ?? false,
        };

        state.ExternalRefs.Add(new ExternalRef
        {
            IntegrationId = _settings.IntegrationId,
            IdentifierKind = RoomStateNormaliser.RoomNumberKind,
            ExternalId = room.RoomId,
        });

        var housekeeping = room.Housekeeping;
        if (housekeeping is null)
        {
            // A room with no status block: its identity is still a fact, and
            // every axis is honestly absent.
            return Normalised(state);
        }

        // Occupancy — OHIP puts these words in the housekeeping field, so they
        // are read as what they are rather than as a condition.
        if (housekeeping.FrontOfficeStatus is not null)
        {
            var occupancy = RoomConditionCodes.ReadCloudOccupancy(housekeeping.FrontOfficeStatus);
            if (occupancy is null)
            {
                return Reject(
                    RejectionReason.UnknownStatus,
                    "frontOfficeStatus",
                    housekeeping.FrontOfficeStatus);
            }

            state.Occupancy = occupancy.Value;
        }

        var condition = ReadCondition(housekeeping.HousekeepingRoomStatus, "housekeepingRoomStatus");
        if (condition.Rejected is not null)
        {
            return condition.Rejected;
        }

        state.Condition = condition.Value;

        // OHIP's own field name on the left, the platform's on the right —
        // `APPS-Q3`. The vendor calls it `housekeepingStatus` and the rejection
        // reason quotes that, because an operator reading it is looking at
        // Oracle's payload; HotelOS calls the department Room Care, and no
        // vendor's vocabulary reaches a platform contract.
        var department = ReadCondition(housekeeping.HousekeepingStatus, "housekeepingStatus");
        if (department.Rejected is not null)
        {
            return department.Rejected;
        }

        state.RoomCareStatus = department.Value;

        var stays = ReadStays(housekeeping.ReservationStatusList);
        if (stays.Rejected is not null)
        {
            return stays.Rejected;
        }

        state.StayStatuses.Add(stays.Lifecycles);

        return Normalised(state);
    }

    /// <summary>
    /// Read one condition axis, distinguishing "not sent" from "sent empty".
    /// </summary>
    private static (RoomCondition Value, NormalisationOutcome? Rejected) ReadCondition(
        string? sourceValue,
        string field)
    {
        // Not sent at all. The axis stays unspecified rather than acquiring a
        // meaning it was never given.
        if (sourceValue is null)
        {
            return (RoomCondition.Unspecified, null);
        }

        var reading = RoomConditionCodes.ReadCloud(sourceValue);

        return reading.TryGet(out var condition)
            ? (condition, null)
            : (RoomCondition.Unspecified,
                Reject(RejectionReason.UnknownStatus, field, reading.UnrecognisedValue));
    }

    private static (List<StayLifecycle> Lifecycles, NormalisationOutcome? Rejected) ReadStays(
        IReadOnlyList<string> statuses)
    {
        var lifecycles = new List<StayLifecycle>();

        foreach (var status in statuses)
        {
            var reading = RoomStayStatusCodes.Read(status, out var contributesStay);
            if (!reading.TryGet(out var lifecycle))
            {
                return (lifecycles,
                    Reject(RejectionReason.UnknownStatus, "reservationStatusList", reading.UnrecognisedValue));
            }

            if (contributesStay)
            {
                lifecycles.Add(lifecycle);
            }
        }

        return (lifecycles, null);
    }

    private NormalisationOutcome Normalised(RoomState state) =>
        new NormalisationOutcome.RoomStateNormalised(new RoomStateFact
        {
            Header = new FactHeader { PropertyId = _settings.PropertyId },
            State = state,
        });

    private static NormalisationOutcome Reject(RejectionReason reason, string field, string? raw) =>
        new NormalisationOutcome.Rejected(reason, field, raw);
}
