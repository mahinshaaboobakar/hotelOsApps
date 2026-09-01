using HotelOS.Contracts.Common.V1;
using PmsOracle.Normalisation;
using PmsOracle.Vocabularies;

namespace PmsOracle.Capabilities;

/// <summary>
/// What the <c>pms-oracle</c> package provides: three integrations, declared.
/// </summary>
/// <remarks>
/// <para>
/// One package, three integrations — ADR 0128 §2, and the owner's
/// <c>CONN-Q2(a)</c>. They share this implementation and nothing else: three
/// transports, two directions, two credential models and four status
/// vocabularies between them. Each is independently identified, configured and
/// health-reported, which is why the identifier is <c>oracle-cloud</c> and
/// never <c>oracle</c> (R28).
/// </para>
/// <para>
/// <b>The vocabularies come from the readers, not from a list beside them.</b>
/// Every value here is the same table the parser consults, so what the setup
/// sheet promises a hotel cannot drift from what the connector accepts. A
/// vocabulary written twice disagrees with itself the first time somebody adds
/// a value in a hurry.
/// </para>
/// </remarks>
public static class PmsOracleCapabilities
{
    /// <summary>The OHIP integration — we pull, and the queue empties as we read.</summary>
    public static IntegrationCapability Cloud { get; } = new(
        IntegrationId: "oracle-cloud",
        Delivery: ChangeDelivery.PolledQueue,
        Dedupe: DedupePromise.EventId,
        Produces: [FactKind.RoomStay, FactKind.RoomState],
        StatusVocabulary: Combined(CloudStayStatus.Declared, RoomStayStatusCodes.Declared,
            RoomConditionCodes.DeclaredCloud),

        // Observed, not closed: OHIP names its own kinds and the values are not
        // yet known, so the connector passes them through and this records what
        // has been seen. Vendor documentation or a live call settles the set.
        IdentifierKinds: [RoomStateNormaliser.RoomNumberKind]);

    /// <summary>The on-site agent integration — the PMS posts to us.</summary>
    public static IntegrationCapability OnPremise { get; } = OnSite("oracle-onpremise");

    /// <summary>The on-site web variant — same wire, different endpoint and credential.</summary>
    public static IntegrationCapability Web { get; } = OnSite("oracle-web");

    /// <summary>All three, for the Hub's registry and for the package's own manifest.</summary>
    public static IReadOnlyList<IntegrationCapability> All { get; } = [Cloud, OnPremise, Web];

    private static IntegrationCapability OnSite(string integrationId) => new(
        IntegrationId: integrationId,
        Delivery: ChangeDelivery.Push,
        Dedupe: DedupePromise.ContentDigest,
        Produces: [FactKind.RoomStay, FactKind.RoomState],
        StatusVocabulary: Combined(OnSiteStayStatus.Declared, RoomStayStatusCodes.Declared,
            RoomConditionCodes.DeclaredOnSite, FrontOfficeCodes.Declared),

        // Known, because the agent sends exactly one identifier per entity.
        IdentifierKinds:
        [
            OnSiteNormaliser.ReservationNumberKind,
            RoomStateNormaliser.RoomNumberKind,
        ]);

    private static IReadOnlyCollection<string> Combined(
        params IReadOnlyCollection<string>[] vocabularies) =>
        vocabularies.SelectMany(v => v).Distinct(StringComparer.Ordinal).ToList();
}
