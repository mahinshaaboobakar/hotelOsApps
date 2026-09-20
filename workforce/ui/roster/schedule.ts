/**
 * What the Staff Schedule is given — one person's month.
 *
 * # It is a manager's screen with the picker on somebody
 *
 * Workforce is a manager and HR application, not one every staff member opens.
 * The self-serve view is **this same screen with the picker fixed to the signed-in
 * person** — one surface, two audiences, which is why nothing here is shaped
 * around who is looking.
 */

/** One day of the month grid. */
export interface ScheduleDay {
  /** The date, or null for a leading or trailing blank. */
  date: number | null;

  /**
   * The day itself, ISO.
   *
   * Carried so the grid can say which cell is today without rebuilding a date
   * from a number and the month's name — and because `date` alone is ambiguous:
   * a leading blank and a real day can both read `28`.
   */
  on: string;

  /** The shift's short code, the leave type's name, or null. */
  mark: string | null;

  /** How it reads. */
  tone: "brand" | "ok" | "warn" | "neutral" | "leave" | null;

  /**
   * The duty this person holds that day, as **ISO instants**.
   *
   * The span, not a flag: the frame prints the hours in the cell, because a
   * duty crossing midnight is the one a person needs the hours of — and the
   * screen says them in the property's clock, because a duty rendered in the
   * server's would put a handover on the wrong side of midnight.
   */
  // Null on a day with no duty — which is what ScheduleView sends; absent on a
  // leading blank. Typed `?: string` it let a null through to the formatter.
  dutyFrom?: string | null;
  dutyTo?: string | null;

  /**
   * Which part of that duty this cell carries — `64g` §5, ruled sent.
   *
   * A duty running 22:00 → 06:00 was drawn on its start day only, so the
   * morning the person was still holding it was blank. The frame draws that
   * tail at reduced weight, because *both dates carry the duty* (WF-Q8).
   *
   * **The service decides which**, because only it knows what day an instant
   * falls on at the property: the screen has a zone for rendering and no way to
   * place a day across one. Null where the cell carries no duty.
   */
  dutyPart?: "starts" | "tail" | null;

  // **No `today` on the cell.** It was one, and only the fixture ever set it —
  // a boolean per cell is six-weeks-of-cells able to disagree with each other.
  // The month carries the day instead, and the cell whose `on` matches is
  // today: one fact, from the one place that knows it (ADR 0211).
}

/** The month. */
export interface Schedule {
  who: string;
  initials: string;
  /**
   * The month, as `YYYY-MM-DD` on its first day — the surface names it.
   *
   * A day rather than an instant: a month is a calendar fact with no zone, and
   * `month-year` moved to `DayStyle` on 2026-09-16 for exactly that reason. Sent
   * as `"MMMM yyyy"` it carried one language's month name to every property.
   */
  month: string;

  /** The four figures above the grid. */
  shifts: number;
  leaveDays: number;

  /** How many duties this month, and when the first begins. */
  duty: number;
  dutyFrom: string | null;
  dutyTo: string | null;

  /**
   * The leave balance — null, which is what the service sends every time.
   *
   * `ScheduleView`: *"the balance sentence belongs to Leave and is read there"*.
   * This was typed `string` and split on spaces, so on a real property the
   * screen threw before drawing a day; only the fixture ever sent one.
   */
  balance: string | null;

  /**
   * The property's operating day, ISO — `64g` §5, ruled sent (ADR 0211).
   *
   * The frame marks today's cell and nothing on the wire said which day that
   * was, so the screen either marked nothing or would have had to ask the
   * machine it happens to be drawn on. Null where Context could not answer:
   * the grid then marks no day, rather than marking the wrong one.
   */
  today: string | null;

  /** Six weeks of seven, Monday first, with blanks at both ends. */
  days: readonly ScheduleDay[];
}

/**
 * A recorded instant at the PROPERTY's hour — Kochi, +05:30.
 *
 * The frames draw 20:00→08:00 at that hotel, so the fixture holds the instant
 * that IS 20:00 there. `20:00Z` would be 01:30 the next morning and would look
 * entirely convincing on the screen.
 */
function at(dayOfAugust: number, localHour: number): string {
  return new Date(
    Date.UTC(2026, 7, dayOfAugust, localHour, 0) - (5 * 60 + 30) * 60_000,
  ).toISOString();
}

/** The ISO day of an August date, which is the month the fixture draws. */
function august(date: number): string {
  return `2026-08-${String(date).padStart(2, "0")}`;
}

/** A working day. */
function work(date: number, mark: string, tone: ScheduleDay["tone"]): ScheduleDay {
  return { date, on: august(date), mark, tone };
}

/** A blank cell from an adjacent month — July here, which is why `on` matters. */
function blank(date: number): ScheduleDay {
  return { date, on: `2026-07-${String(date).padStart(2, "0")}`, mark: null, tone: null };
}

export const recordedSchedule: Schedule = {
  who: "Anjali Menon",
  initials: "AM",
  month: "2026-08-01",

  shifts: 22,
  leaveDays: 2,
  duty: 1,
  dutyFrom: at(28, 20),
  dutyTo: at(29, 8),
  // What the service sends. This carried "4 / 8 casual remaining".
  balance: null,

  // The day the frame is drawn on — the 28th, which is the day the duty starts,
  // so the marked cell and the duty's own cell are the same one.
  today: "2026-08-28",

  days: [
    blank(28), blank(29), blank(30), blank(31),
    work(1, "M", "brand"), work(2, "M", "brand"), work(3, "OFF", "neutral"),
    work(4, "A", "ok"), work(5, "A", "ok"), work(6, "A", "ok"),
    work(7, "Casual", "leave"), work(8, "Casual", "leave"),
    work(9, "M", "brand"), work(10, "OFF", "neutral"),
    work(11, "M", "brand"), work(12, "SB", "warn"), work(13, "M", "brand"),
    work(14, "M", "brand"), work(15, "M", "brand"), work(16, "A", "ok"),
    work(17, "OFF", "neutral"),
    work(18, "A", "ok"), work(19, "A", "ok"), work(20, "A", "ok"),
    work(21, "A", "ok"), work(22, "M", "brand"), work(23, "OFF", "neutral"),
    work(24, "M", "brand"),
    work(25, "M", "brand"), work(26, "M", "brand"), work(27, "M", "brand"),
    // The duty crosses midnight, so both dates carry it: the badge names its
    // span on the 28th and the 29th carries the tail. Both are the service's
    // now — `ScheduleView` sends `dutyPart`, and the day the grid marks comes
    // from the month rather than from a boolean on a cell.
    {
      date: 28, on: august(28), mark: "M", tone: "brand",
      dutyFrom: at(28, 20), dutyTo: at(29, 8), dutyPart: "starts",
    },
    {
      date: 29, on: august(29), mark: "OFF", tone: "neutral",
      dutyFrom: at(28, 20), dutyTo: at(29, 8), dutyPart: "tail",
    },
    work(30, "A", "ok"), work(31, "A", "ok"),
  ],
};
