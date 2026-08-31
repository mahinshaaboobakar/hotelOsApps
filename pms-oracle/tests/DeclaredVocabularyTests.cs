using PmsOracle.Vocabularies;
using Xunit;

namespace PmsOracle.Tests;

/// <summary>
/// The guard on the declared lists: what the setup sheet promises a hotel is
/// exactly what the parser accepts.
/// </summary>
/// <remarks>
/// <para>
/// Section 4 of the on-site setup sheet tells the hotel's OPERA operator which
/// status values HotelOS accepts, and warns that anything else is rejected and
/// named back. That promise is only worth making if the list and the reader
/// cannot disagree.
/// </para>
/// <para>
/// They cannot, because each vocabulary declares its values once and exposes
/// the same table both ways. These tests assert that the arrangement still
/// holds — a guard on the derivation, not a second copy of the list. Spelling
/// the values out again here would be a tautology: the test would pass by
/// agreeing with itself.
/// </para>
/// </remarks>
public sealed class DeclaredVocabularyTests
{
    [Fact]
    public void every_value_ohip_declares_is_one_its_reader_recognises()
    {
        Assert.NotEmpty(CloudStayStatus.Declared);

        foreach (var declared in CloudStayStatus.Declared)
        {
            Assert.True(
                CloudStayStatus.Read(declared).Recognised,
                $"declared but not readable: {declared}");
        }
    }

    [Fact]
    public void every_value_the_on_site_flavours_declare_is_one_their_reader_recognises()
    {
        Assert.NotEmpty(OnSiteStayStatus.Declared);

        foreach (var declared in OnSiteStayStatus.Declared)
        {
            Assert.True(
                OnSiteStayStatus.Read(declared).Recognised,
                $"declared but not readable: {declared}");
        }
    }

    /// <summary>
    /// The on-site pair must both survive into the declared list, because the
    /// setup sheet has to ask the hotel's agent to send both halves.
    /// </summary>
    [Fact]
    public void both_halves_of_a_check_in_are_declared()
    {
        Assert.Contains("Checked In", OnSiteStayStatus.Declared);
        Assert.Contains("CHECKED IN", OnSiteStayStatus.Declared);
    }

    /// <summary>
    /// The two flavours' vocabularies are genuinely different, which is
    /// requirement R5's point and the reason the mapping is per integration
    /// rather than per vendor.
    /// </summary>
    [Fact]
    public void the_cloud_and_on_site_vocabularies_share_no_value()
    {
        Assert.Empty(CloudStayStatus.Declared.Intersect(OnSiteStayStatus.Declared));
    }
}
