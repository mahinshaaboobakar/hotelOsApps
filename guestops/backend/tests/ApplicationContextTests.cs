using HotelOS.GuestOps.Infrastructure.Platform;
using HotelOS.Platform;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// An onward call from this application carries no user identity — AUTHZ-Q18.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every GuestOps operation failed for nine days on the absence of this.</b>
/// The Context Service refused with <i>"a Application connection may not supply a
/// user identity; present an access token instead"</i>, and <c>IBusinessDay</c>
/// sits behind availability, booking, stay matching, registration, assignment and
/// the stay list — so one refusal stopped all of them.
/// </para>
/// <para>
/// <b>The scope deliberately carries a user here.</b> That is the arrangement
/// that matters: a desk clerk really is behind a GuestOps call, so the inbound
/// scope is right to name them, and the defect was propagating that name onward.
/// A fixture with no user would pass under the broken factory call too — the
/// case that cannot tell the two apart, which is why the user is present and
/// asserted absent from the ENVELOPE rather than from the scope.
/// </para>
/// </remarks>
public sealed class ApplicationContextTests
{
    private static readonly Guid Property = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Organization = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Desk = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Session = Guid.Parse("44444444-4444-4444-4444-444444444444");

    /// <summary>A desk clerk's scope — a real user, which is what makes this a test.</summary>
    private static RequestScope DeskScope() => new()
    {
        Caller = CallerKind.User,
        TransportPrincipalKind = TransportPrincipalKind.Desktop,
        PropertyId = Property,
        OrganizationId = Organization,
        UserId = Desk,
        SessionId = Session,
    };

    [Fact]
    public void The_envelope_names_no_user_even_though_a_person_is_behind_the_call()
    {
        var context = ApplicationContext.AsItself(DeskScope());

        Assert.Null(context.User);
    }

    /// <summary>
    /// Not merely absent — a session is the half that let MFA and a Kernel restart
    /// fail, so it is asserted separately rather than inside a null check.
    /// </summary>
    [Fact]
    public void And_carries_no_session()
    {
        var context = ApplicationContext.AsItself(DeskScope());

        Assert.DoesNotContain(Session.ToString(), context.ToString());
        Assert.DoesNotContain(Desk.ToString(), context.ToString());
    }

    /// <summary>
    /// It names this application as itself — the arm `Propagate` accepts.
    /// </summary>
    /// <remarks>
    /// <c>RequestScopeExtensions.Propagate</c> returns the caller when
    /// <c>context.User</c> is null. A service envelope is therefore the open path
    /// and not a workaround: asserted so a later change to "no envelope at all"
    /// has to meet the reason this one was chosen.
    /// </remarks>
    [Fact]
    public void It_acts_as_this_application()
    {
        var context = ApplicationContext.AsItself(DeskScope());

        Assert.NotNull(context.Service);
        Assert.Equal("guestops", context.Service.ServiceName);
    }

    /// <summary>
    /// The property and organization still travel — the operation runs against them.
    /// </summary>
    /// <remarks>
    /// Stripping the user must not strip the scope. A context with no property
    /// would be refused for a different reason and would read, to whoever debugged
    /// it next, as the same defect returning.
    /// </remarks>
    [Fact]
    public void The_property_and_organization_still_travel()
    {
        var context = ApplicationContext.AsItself(DeskScope());

        Assert.Equal(Property.ToString(), context.PropertyId);
        Assert.Equal(Organization.ToString(), context.OrganizationId);
    }
}
