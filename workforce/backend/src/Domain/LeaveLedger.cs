namespace HotelOS.Workforce.Domain;

/// <summary>Why a balance moved.</summary>
/// <remarks>
/// The ledger's vocabulary. Every entry says which of these it is, because a
/// balance that cannot explain itself is a number somebody will dispute and
/// nobody can defend.
/// </remarks>
public enum LeaveLedgerKind
{
    /// <summary>The monthly rate, credited.</summary>
    Accrual = 0,

    /// <summary>An approved request, debited.</summary>
    Approval = 1,

    /// <summary>An approved request cancelled, credited back.</summary>
    Cancellation = 2,

    /// <summary>HR putting the number where it should be.</summary>
    /// <remarks>
    /// The manual floor, exactly as manual attendance is: every accrual rule
    /// meets a case it did not anticipate, and a system with no correction gets
    /// corrected in a spreadsheet instead. Recorded and attributed, never a
    /// silent overwrite.
    /// </remarks>
    Adjustment = 3,
}

/// <summary>One movement of one person's balance.</summary>
/// <remarks>
/// <para>
/// <b>The balance is a ledger, not a counter.</b> Accrual credits it, approval
/// debits it, cancelling an approved request credits it back — and the balance
/// itself is the sum, computed when asked. A stored total would be a
/// clock-dependent value of the same family this application has now refused
/// five times.
/// </para>
/// <para>
/// It is also what makes a balance defensible. <i>"You have four days"</i> is an
/// assertion; four rows saying where they came from is a record.
/// </para>
/// </remarks>
public class LeaveLedgerEntry
{
    /// <summary>This entry's own identity.</summary>
    public Guid Id { get; set; }

    /// <summary>The tenancy boundary.</summary>
    public Guid PropertyId { get; set; }

    /// <summary>Whose balance moved.</summary>
    public Guid StaffId { get; set; }

    /// <summary>Which kind of leave.</summary>
    public Guid LeaveTypeId { get; set; }

    /// <summary>Days, signed: positive credits, negative debits.</summary>
    /// <remarks>
    /// One signed column rather than separate credit and debit columns, because
    /// two columns admit a row that is both and a row that is neither.
    /// </remarks>
    public decimal Days { get; set; }

    /// <summary>Why it moved.</summary>
    public LeaveLedgerKind Kind { get; set; }

    /// <summary>The day it is attributed to.</summary>
    public DateOnly OccurredOn { get; set; }

    /// <summary>The request that caused it, when one did.</summary>
    public Guid? LeaveRequestId { get; set; }

    /// <summary>Who recorded it, for an adjustment.</summary>
    public Guid? RecordedByUserId { get; set; }

    /// <summary>Why, in words — required for an adjustment.</summary>
    public string Note { get; set; } = string.Empty;

    /// <summary>When the entry was written.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
