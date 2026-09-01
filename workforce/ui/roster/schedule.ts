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
   * The duty this person holds that day, as its span reads.
   *
   * The span, not a flag: the frame prints "MOD 20:00→08:00" in the cell,
   * because a duty crossing midnight is the one a person needs the hours of.
   */
  duty?: string;

  /** The day the register is being read on, drawn with an emphasised border. */
  today?: boolean;

  /**
   * The tail of a duty that began the day before, at reduced weight.
   *
   * A duty running 20:00→08:00 belongs to two dates, so the second one shows
   * where it ends — otherwise the grid says the duty stopped at midnight.
   */
  tail?: string;
}

/** The month. */
export interface Schedule {
  who: string;
  initials: string;
  month: string;

  /** The four figures above the grid. */
  shifts: number;
  leaveDays: number;
  duty: string;
  balance: string;

  /** Six weeks of seven, Monday first, with blanks at both ends. */
  days: readonly ScheduleDay[];
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
  month: "August 2026",

  shifts: 22,
  leaveDays: 2,
  duty: "1 MOD duty · Fri 28, 20:00–08:00",
  balance: "4 / 8 casual remaining",

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
    // The duty crosses midnight, so it is drawn on BOTH dates — the badge names
    // its span here, and the 29th carries the tail. A duty running 20:00→08:00
    // genuinely belongs to two dates (WF-Q8), and a month grid that showed it on
    // one would be the per-day shape the ruling refused.
    { date: 28, mark: "M", tone: "brand", duty: "MOD 20:00→08:00", today: true },
    { date: 29, mark: "OFF", tone: "neutral", tail: "…08:00" },
    work(30, "A", "ok"), work(31, "A", "ok"),
  ],
};;
