using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Grpc;
using HotelOS.Platform;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// The gRPC door's lifecycle conversion — a nameless value is refused by name.
/// </summary>
/// <remarks>
/// <para>
/// <b>The repair this covers, and what it used to be.</b> <c>CorrectStay</c> read
/// <c>(Domain.StayLifecycle)(int)request.To</c>, a bare double cast, so a caller
/// sending <c>STAY_LIFECYCLE_UNSPECIFIED = 0</c> — or any number — reached
/// <c>CorrectAsync</c>, which validates the reason and not the target. The stay was
/// stored with a lifecycle no name describes, and <c>stay.corrected</c> announced a
/// <c>to</c> with no name. <i>Found by driving the enum axis in
/// <c>PartCCorrectStayDriver</c>, which recorded the old behaviour and endorsed
/// none of it.</i>
/// </para>
/// <para>
/// <b>The module door never had this hole</b>, and saying so is why the finding is
/// about one door: <c>CorrectCommand</c> takes the lifecycle as a <i>string</i> and
/// refuses anything that is not an exact member name —
/// <c>CorrectCommandTests</c> drives <c>"Levitating"</c> and <c>"inhouse"</c>. A
/// desk could never express zero. <b>A connector could.</b>
/// </para>
/// <para>
/// <b>These are wire-conversion tests and need no database</b>, which is why they
/// are not in a driver: Part C creates data, and this is a mapping.
/// </para>
/// </remarks>
public sealed class LifecycleWireTests
{
    /// <summary>Every named member crosses the wire unchanged.</summary>
    /// <remarks>
    /// <b>Enumerated from the DOMAIN enum, so a member added tomorrow is covered the
    /// day it is declared.</b> Asserting the name rather than the number is what
    /// makes this catch a renumbering: a cast that mapped <c>Booked</c> onto
    /// <c>Pending</c> would satisfy a numeric comparison on both sides.
    /// </remarks>
    [Fact]
    public void Every_named_lifecycle_crosses_the_wire_as_itself()
    {
        foreach (var member in Enum.GetValues<StayLifecycle>())
        {
            var onTheWire = (Contracts.V1.StayLifecycle)(int)member;

            Assert.Equal(member, GuestOpsGrpcService.FromProto(onTheWire));
        }
    }

    /// <summary>
    /// The two enums agree member for member — which is what the derived guard
    /// assumes and must not merely trust.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>FromProto</c> is a cast plus <c>Enum.IsDefined</c>, deliberately: it
    /// asks the domain what it declares instead of holding a seven-arm switch that
    /// would be a hand-kept copy of the thing it converts.</b> The price of that is
    /// one assumption — that the numbers line up — and a renumbering would make the
    /// door silently map one state onto another with nothing failing.
    /// </para>
    /// <para>
    /// So this is the <i>two things that must agree</i> check: for every domain
    /// member, the wire member with the same number carries the same NAME. It is
    /// derived from both enums, so neither side can drift alone.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_wire_and_the_domain_name_the_same_lifecycle_for_each_number()
    {
        foreach (var member in Enum.GetValues<StayLifecycle>())
        {
            var onTheWire = (Contracts.V1.StayLifecycle)(int)member;

            // STAY_LIFECYCLE_IN_HOUSE ⇄ InHouse: the wire's SCREAMING_SNAKE with its
            // enum prefix, against the domain's PascalCase.
            var wireName = onTheWire.ToString()
                .Replace("StayLifecycle", string.Empty, StringComparison.Ordinal);

            Assert.Equal(member.ToString(), wireName);
        }
    }

    /// <summary>
    /// <c>STAY_LIFECYCLE_UNSPECIFIED</c> is refused by name — the repair itself.
    /// </summary>
    /// <remarks>
    /// The sentence names the field and quotes what arrived, because a caller told
    /// only <i>invalid request</i> has to guess which of five fields it was.
    /// </remarks>
    [Fact]
    public void An_unspecified_lifecycle_is_refused_by_name()
    {
        var refused = Assert.Throws<InvalidRequestException>(
            () => GuestOpsGrpcService.FromProto(Contracts.V1.StayLifecycle.Unspecified));

        Assert.Contains("to is required", refused.Message, StringComparison.Ordinal);
        Assert.Contains("Unspecified", refused.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// And so is a number no version of this contract has ever declared.
    /// </summary>
    /// <remarks>
    /// <b>This is what <c>Enum.IsDefined</c> buys over refusing zero alone.</b> A
    /// newer client sending <c>8</c> is exactly as nameless as one sending <c>0</c>,
    /// and a guard written only against <c>Unspecified</c> would have stored it.
    /// </remarks>
    [Theory]
    [InlineData(8)]
    [InlineData(99)]
    [InlineData(-1)]
    public void A_lifecycle_this_contract_never_declared_is_refused(int number)
    {
        var refused = Assert.Throws<InvalidRequestException>(
            () => GuestOpsGrpcService.FromProto((Contracts.V1.StayLifecycle)number));

        Assert.Contains("names none", refused.Message, StringComparison.Ordinal);
    }
}
