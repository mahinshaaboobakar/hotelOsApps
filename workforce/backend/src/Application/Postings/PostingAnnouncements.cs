using System.Text.Json.Serialization;

namespace HotelOS.Workforce.Application.Postings;

/// <summary>What every posting announcement carries.</summary>
/// <remarks>
/// <para>
/// <b>Both department identifiers, deliberately</b> — chapter 04 §2. The id is
/// what <c>department:{uuid}</c> needs; the code is what the fact <i>means</i> and
/// what survives a database being rebuilt. Carrying only the id makes the
/// announcement unreadable to a human debugging it; carrying only the code makes
/// it unusable to the consumer that must write a tuple.
/// </para>
/// <para>
/// <b>Every wire name is stated, never left to a convention.</b>
/// <c>EventAppender</c> calls <c>JsonSerializer.SerializeToDocument</c> with no
/// options, so a property named <c>UserId</c> reaches the store as
/// <c>"UserId"</c> — while the Kernel reads <c>uuid(body, "user_id")</c>
/// (<c>plan.rs:248</c>). It would find nothing, no tuple would be written, and
/// the event would be stored, relayed and acknowledged exactly as though it had
/// worked. <b>That is CC's §2 failure arriving through a different door</b>, and
/// the Knowledge Service's <c>FolderAccessGranted</c> carries these same
/// attributes against the same hazard.
/// </para>
/// </remarks>
public sealed record PostingAnnouncement
{
    /// <summary>Identity's user — the principal the tuple names.</summary>
    /// <remarks>
    /// <b>In the body, never the envelope.</b> The envelope names whoever made
    /// the decision, who is a different person in every case that matters:
    /// reading it there would post the supervisor, and would look correct in
    /// every test where the two happen to be the same account.
    /// </remarks>
    [JsonPropertyName("user_id")]
    public required Guid UserId { get; init; }

    /// <summary>Master Data's person — for a consumer that is not the graph.</summary>
    [JsonPropertyName("staff_id")]
    public required Guid StaffId { get; init; }

    /// <summary>The department ROW id, which is what the tuple addresses.</summary>
    [JsonPropertyName("department_id")]
    public required Guid DepartmentId { get; init; }

    /// <summary>The canon code — ADR 0119, what reports group on.</summary>
    [JsonPropertyName("department_code")]
    public required string DepartmentCode { get; init; }

    /// <summary>So a consumer can correlate an end with its start.</summary>
    [JsonPropertyName("posting_id")]
    public required Guid PostingId { get; init; }

    /// <summary>The tenancy boundary.</summary>
    [JsonPropertyName("property_id")]
    public required Guid PropertyId { get; init; }

    /// <summary>When.</summary>
    [JsonPropertyName("occurred_at")]
    public required DateTimeOffset OccurredAt { get; init; }
}

/// <summary>
/// The four events this application announces about postings.
/// </summary>
/// <remarks>
/// <para>
/// <b>The ratified <c>AUTHZ-Q20</c> contract</b> —
/// <c>workforce/docs/chapters/04-the-announcement-contract.md</c>, agreed with
/// the Kernel stream and ratified 2026-09-01. This type is the whole of this
/// application's half, in one place, so the contract has a home rather than
/// four call sites.
/// </para>
/// <para>
/// <b>Domain <c>user</c>, aggregate <c>posting</c>, and they differ
/// deliberately.</b> The fact is about a user — <i>this person now works in
/// Front Office</i> — and ADR 0006 routes by what an event means, so it belongs
/// beside <c>user.assigned</c> where consumers already listen. The record that
/// establishes it is a posting, which is what this application owns and the only
/// row that can lend a version.
/// </para>
/// <para>
/// The Kernel's <c>GrantKind</c> now names the routed domain and the aggregate
/// type <b>separately</b> because of that split — CC's §2 finding, which is why
/// the subscription hears these at all. Before it, the filter was built from the
/// aggregate type and would have subscribed to
/// <c>property.*.posting.posted.&gt;</c>, which nothing publishes.
/// </para>
/// </remarks>
public static class PostingAnnouncements
{
    /// <summary>A person gained a place in a department.</summary>
    public const string Posted = "user.posted";

    /// <summary>That place ended.</summary>
    public const string PostingEnded = "user.posting_ended";

    /// <summary>A person now heads a department.</summary>
    /// <remarks>
    /// Named to mirror the pair it joins rather than the graph it ends in.
    /// <c>headship_granted</c> was the obvious alternative and is refused: that
    /// is authorization vocabulary, and the fact is that somebody heads a
    /// department — which merely <i>has</i> an authorization consequence.
    /// </remarks>
    public const string HeadshipStarted = "user.headship_started";

    /// <summary>They no longer head it.</summary>
    public const string HeadshipEnded = "user.headship_ended";

    /// <summary>The aggregate every one of them is announced against.</summary>
    /// <remarks>
    /// <c>AUTHZ-Q20</c> and <c>HUB-Q4</c>: announce against what you own. The
    /// posting has its own version sequence, so no per-person collision and no
    /// foreign row bumped.
    /// </remarks>
    public const string Aggregate = "posting";
}
