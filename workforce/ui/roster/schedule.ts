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
   * The day the register is being read on, drawn with an emphasised border.
   *
   * **The service does not send it** (`ScheduleView.Calendar`); only the fixture
   * did. Owed, recorded in the audit — the screen draws it the day one arrives.
   */
  today?: boolean;

  // **No `tail`.** The second date of a duty crossing midnight carried a
  // composed clock ("…08:00") that only the fixture ever sent — ScheduleView
  // has no such field. The value it would say is the previous day's `dutyTo`,
  // in the property's zone, and nothing here can yet say which calendar day an
  // instant falls on in a zone: that needs either the service to send the tail
  // day's end or an SDK helper. Owed, both arms in the audit, neither chosen.
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

/** A working day. */
function work(date: number, mark: string, tone: ScheduleDay["tone"]): ScheduleDay {
  return { date, mark, tone };
}

/** A blank cell from an adjacent month. */
function blank(date: number): ScheduleDay {
  return { date, mark: null, tone: null };
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
    // The duty crosses midnight; the badge names its span on the day it starts.
    // The 29th carried a tail ("…08:00") and the 28th `today: true` — neither
    // is anything the service sends, so neither is here (see `ScheduleDay`).
    {
      date: 28, mark: "M", tone: "brand",
      dutyFrom: at(28, 20), dutyTo: at(29, 8),
    },
    work(29, "OFF", "neutral"),
    work(30, "A", "ok"), work(31, "A", "ok"),
  ],
};
