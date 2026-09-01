using HotelOS.Workforce.Application.Postings;

namespace HotelOS.Workforce.Application.Capabilities;

/// <summary>Record something a person can do.</summary>
public sealed record RecordCapabilityCommand
{
    /// <summary>Master Data's person.</summary>
    public required Guid StaffId { get; init; }

    /// <summary>What they can do.</summary>
    public required string Name { get; init; }

    /// <summary>The last day it is valid, or null when it does not lapse.</summary>
    /// <remarks>
    /// Null is the ordinary case: most capabilities are abilities. Typing a date
    /// is the only thing that makes this a certification, which is the whole of
    /// the design.
    /// </remarks>
    public DateOnly? ValidUntil { get; init; }

    /// <summary>A certificate number, an issuer, or nothing.</summary>
    public string Note { get; init; } = string.Empty;
}

/// <summary>Amend a capability — including renewing it.</summary>
/// <remarks>
/// <b>The person cannot be patched.</b> A capability recorded against the wrong
/// staff member is removed and recorded again: moving it would silently transfer
/// a certificate between two people in the register, and the register is the
/// document a safety inspector reads.
/// </remarks>
public sealed record AmendCapabilityCommand
{
    /// <summary>The capability to amend.</summary>
    public required Guid Id { get; init; }

    /// <summary>The version the caller read.</summary>
    public required long ExpectedVersion { get; init; }

    /// <summary>New name, or null to leave it.</summary>
    public string? Name { get; init; }

    /// <summary>New note, or null to leave it.</summary>
    public string? Note { get; init; }

    /// <summary>
    /// Present with a date renews it; present with null turns a certification
    /// back into an ability; absent leaves it alone.
    /// </summary>
    /// <remarks>
    /// Three outcomes from one field, which a nullable date could only express
    /// as two. Renewal is the common one and clearing is rare — but a capability
    /// wrongly given an expiry has to be correctable without deleting the record
    /// somebody may already have cited.
    /// </remarks>
    public Optional<DateOnly?> ValidUntil { get; init; }
}

/// <summary>Remove a capability recorded in error.</summary>
/// <remarks>
/// Not a renewal and not a lapse. A capability that has expired stays on the
/// register as expired — that is the point of the register — and this is for the
/// row that should never have been there.
/// </remarks>
public sealed record RemoveCapabilityCommand
{
    /// <summary>The capability to remove.</summary>
    public required Guid Id { get; init; }

    /// <summary>The version the caller read.</summary>
    public required long ExpectedVersion { get; init; }
}

/// <summary>Which capabilities to list.</summary>
public sealed record ListCapabilitiesQuery
{
    /// <summary>Only this person's. Null means the whole property.</summary>
    public Guid? StaffId { get; init; }
}

/// <summary>Whose capabilities need attention.</summary>
public sealed record AttentionQuery
{
    /// <summary>
    /// Only people posted to this canon department. Null means the property.
    /// </summary>
    /// <remarks>
    /// Resolved through postings — the same resolution the leave approver uses,
    /// which is what makes "the department head's list" mean the people they are
    /// actually responsible for rather than everyone.
    /// </remarks>
    public string? DepartmentCode { get; init; }
}
