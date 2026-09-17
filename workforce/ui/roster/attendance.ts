/**
 * What the Attendance screen is given — shapes, and the frames' facts.
 *
 * # The day comparison is a union, not a join
 *
 * Rows come from **either** side: somebody rostered who did not appear, and
 * somebody who appeared unrostered, are the two rows the screen exists to show.
 * Joining on the rota would hide the second; joining on attendance would hide
 * the first.
 */

/** Where a record came from — evidence, or an assertion. */
export type Source = "manual" | "device" | "mobile";

/** One person's day, planned beside actual. */
export interface DayRow {
  who: string;
  role: string;

  /**
   * Whether the rota planned anything for this person today.
   *
   * **Separate from {@link DayRow.postedAt}, because it was one string holding
   * three states** — a clock, the word `rostered`, and null. A surface given
   * that cannot tell a time it must render from a word it must not, and could
   * not say the word in any language but the service's (ADR 0175).
   */
  rostered: boolean;

  /**
   * When the rota expected them, as a clock string — null when it planned a
   * day without a start, and null when it planned nothing.
   *
   * **The doc here used to promise "the shift short code and its start", and
   * the fixture carried `"M 07:00"` to match. The service sends neither**:
   * `DayComparison.DayRow` carries a department code, a rostered flag and a
   * scheduled start, and no shift code at all. Corrected to what crosses rather
   * than to what the screen would like — the code is a real gap and is reported
   * rather than invented here.
   */
  postedAt: string | null;

  /** When they arrived, as a clock string, or null when nobody recorded it. */
  in: string | null;

  /** When they left, or null while the shift is open. */
  out: string | null;

  /**
   * What the two facts add up to — **derived, never stored**.
   *
   * `WF-Q10`: 07:00 posted and 07:20 clocked are the facts; *"Late 20 min"* is
   * arithmetic over them, and a stored late-minutes column is a number that can
   * disagree with the two times beside it.
   */
  against: string;

  /** How that reads. */
  tone: "ok" | "warn" | "bad" | "neu";

  /**
   * Where the record came from, or null when there is no record.
   *
   * **Not decoration** — it is the difference between evidence and an
   * assertion, and a device record names a reading rather than a person.
   */
  source: Source | null;
}

/** The day, as the screen draws it. */
export interface Day {
  date: string;
  department: string;
  rows: readonly DayRow[];
}

export const recordedDay: Day = {
  date: "Friday 28 August · business day",
  department: "Front Office",
  rows: [
    {
      who: "Priya Thomas", role: "Supervisor · Zone 1", rostered: true, postedAt: "07:00",
      in: "06:52", out: "15:04", against: "On time", tone: "ok", source: "manual",
    },
    {
      who: "Anjali Menon", role: "Receptionist · Zone 3", rostered: true, postedAt: "07:00",
      in: "07:20", out: "15:10", against: "Late 20 min", tone: "warn", source: "manual",
    },
    {
      who: "Vishnu Das", role: "Night auditor · Zone 1", rostered: true, postedAt: "23:00",
      in: "22:55", out: null, against: "On shift", tone: "neu", source: "manual",
    },
    {
      who: "Sneha Iyer", role: "Receptionist · Zone 2", rostered: true, postedAt: "15:00",
      in: "15:38", out: "23:02", against: "Late 38 min", tone: "warn", source: "manual",
    },
    // Rostered, and nobody recorded them arriving. A record with no arrival says
    // somebody looked; no record at all would say only that nobody looked.
    {
      who: "Rani Rajan", role: "Guest relations · Zone 2", rostered: true, postedAt: "15:00",
      in: null, out: null, against: "Absent", tone: "bad", source: null,
    },
    // The row that matters most: attendance contradicting the rota. Both facts
    // are kept and the discrepancy is shown, never silently reconciled.
    {
      who: "Joseph Kurian", role: "Bell captain · Zone 1", rostered: false, postedAt: null,
      in: "09:05", out: "17:30", against: "Present, not rostered",
      tone: "warn", source: "manual",
    },
  ],
};
