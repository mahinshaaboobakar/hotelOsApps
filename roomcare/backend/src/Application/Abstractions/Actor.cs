using HotelOS.Platform;
using HotelOS.RoomCare.Domain;

namespace HotelOS.RoomCare.Application.Abstractions;

/// <summary>Who is acting, taken from the authenticated scope — never from a request body (§7.5).</summary>
/// <remarks>
/// A person asking through HosPilot is still that person (S7): the kind is
/// <c>USER</c> and <see cref="Via"/> says <c>HOSPILOT</c>, from the scope's
/// actor type <c>ACTOR_TYPE_AGENT</c>.
/// </remarks>
public sealed record Actor(string Kind, Guid? Id, string Via)
{
    private const int AgentActorType = 3;

    /// <summary>The scheduler's tick — nobody asked, so nobody is recorded as having asked.</summary>
    public static readonly Actor System = new(ActorKind.System, null, Domain.Via.App);

    /// <summary>The actor a scope represents.</summary>
    public static Actor Of(RequestScope scope) =>
        scope.UserId is { } user
            ? new Actor(ActorKind.User, user, scope.ActorType == AgentActorType ? Domain.Via.HosPilot : Domain.Via.App)
            : System;

    /// <summary>The person, or a refusal naming the act that needed one.</summary>
    public static Guid PersonOf(RequestScope scope, string act) =>
        scope.UserId ?? throw new InvalidRequestException($"{act} is a person's act and this call named no user");
}
