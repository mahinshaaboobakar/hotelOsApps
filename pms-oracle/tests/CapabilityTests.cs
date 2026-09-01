using HotelOS.Contracts.Common.V1;
using PmsOracle.Capabilities;
using PmsOracle.Vocabularies;
using Xunit;

namespace PmsOracle.Tests;

/// <summary>
/// What the package declares about itself — and the guard that the declaration
/// and the parsers cannot disagree.
/// </summary>
public sealed class CapabilityTests
{
    /// <summary>
    /// R28, and ADR 0020's closed set: three integrations, three identities.
    /// A single <c>oracle</c> identity would make every Oracle event's
    /// provenance ambiguous.
    /// </summary>
    [Fact]
    public void the_package_declares_three_distinct_integrations()
    {
        var ids = PmsOracleCapabilities.All.Select(c => c.IntegrationId).ToList();

        Assert.Equal(3, ids.Count);
        Assert.Equal(3, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.DoesNotContain("oracle", ids);
    }

    /// <summary>
    /// The distinction the round turned on: one source is emptied by reading
    /// it, and the Hub has to know that before it decides what a failed batch
    /// means.
    /// </summary>
    [Fact]
    public void only_the_cloud_integration_reads_a_queue_that_empties()
    {
        Assert.Equal(ChangeDelivery.PolledQueue, PmsOracleCapabilities.Cloud.Delivery);
        Assert.Equal(ChangeDelivery.Push, PmsOracleCapabilities.OnPremise.Delivery);
        Assert.Equal(ChangeDelivery.Push, PmsOracleCapabilities.Web.Delivery);
    }

    /// <summary>
    /// ADR 0128 §5. OHIP promises a stable event id; the on-site agent promises
    /// nothing, so a content digest is the key.
    /// </summary>
    [Fact]
    public void each_integration_declares_what_its_source_can_actually_promise()
    {
        Assert.Equal(DedupePromise.EventId, PmsOracleCapabilities.Cloud.Dedupe);
        Assert.Equal(DedupePromise.ContentDigest, PmsOracleCapabilities.OnPremise.Dedupe);
    }

    /// <summary>
    /// The guard that makes the setup sheet trustworthy: every value the
    /// package advertises is one its parsers accept. Spelling the values out
    /// again here would be a test that passes by agreeing with itself, so this
    /// asserts the derivation instead.
    /// </summary>
    [Fact]
    public void every_declared_on_site_status_is_read_by_one_of_the_on_site_readers()
    {
        foreach (var value in PmsOracleCapabilities.OnPremise.StatusVocabulary)
        {
            var read = OnSiteStayStatus.Read(value).Recognised
                || RoomStayStatusCodes.Read(value, out _).Recognised
                || RoomConditionCodes.ReadOnSite(value).Recognised
                || FrontOfficeCodes.Read(value).Recognised;

            Assert.True(read, $"declared but unreadable: {value}");
        }
    }

    [Fact]
    public void every_declared_cloud_status_is_read_by_one_of_the_cloud_readers()
    {
        foreach (var value in PmsOracleCapabilities.Cloud.StatusVocabulary)
        {
            var read = CloudStayStatus.Read(value).Recognised
                || RoomStayStatusCodes.Read(value, out _).Recognised
                || RoomConditionCodes.ReadCloud(value).Recognised;

            Assert.True(read, $"declared but unreadable: {value}");
        }
    }

    /// <summary>
    /// The empty string is a declared value, not an oversight — a room needing
    /// a pick-up. It has to survive into the advertised vocabulary or the setup
    /// sheet would tell a hotel we reject something we accept.
    /// </summary>
    [Fact]
    public void the_cloud_vocabulary_includes_the_empty_condition()
    {
        Assert.Contains(string.Empty, PmsOracleCapabilities.Cloud.StatusVocabulary);
    }

    /// <summary>
    /// Both on-site casings of a check-in are advertised, because the setup
    /// sheet has to ask the hotel's agent to send both halves.
    /// </summary>
    [Fact]
    public void both_halves_of_a_check_in_are_advertised()
    {
        Assert.Contains("Checked In", PmsOracleCapabilities.OnPremise.StatusVocabulary);
        Assert.Contains("CHECKED IN", PmsOracleCapabilities.OnPremise.StatusVocabulary);
    }

    /// <summary>
    /// The two on-site flavours share a wire and differ only in identity, so
    /// their declarations should differ only there too.
    /// </summary>
    [Fact]
    public void the_two_on_site_flavours_differ_only_by_identity()
    {
        var onPremise = PmsOracleCapabilities.OnPremise;
        var web = PmsOracleCapabilities.Web;

        Assert.NotEqual(onPremise.IntegrationId, web.IntegrationId);
        Assert.Equal(onPremise.Delivery, web.Delivery);
        Assert.Equal(onPremise.Dedupe, web.Dedupe);
        Assert.Equal(onPremise.StatusVocabulary, web.StatusVocabulary);
        Assert.Equal(onPremise.IdentifierKinds, web.IdentifierKinds);
    }

    [Fact]
    public void every_integration_produces_both_fact_kinds()
    {
        Assert.All(PmsOracleCapabilities.All, c =>
        {
            Assert.Contains(FactKind.RoomStay, c.Produces);
            Assert.Contains(FactKind.RoomState, c.Produces);
        });
    }
}
