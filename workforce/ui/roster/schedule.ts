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

  /** True where this person also holds the duty that day. */
  duty: boolean;
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
  return { date, mark, tone, duty: false };
}

/** A blank cell from an adjacent month. */
function blank(date: number): ScheduleDay {
  return { date, mark: null, tone: null, duty: false };
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
    work(11, "M", "brand"), work(12, "M", "brand"), work(13, "M", "brand"),
    work(14, "M", "brand"), work(15, "A", "ok"), work(16, "A", "ok"),
    work(17, "OFF", "neutral"),
    work(18, "M", "brand"), work(19, "M", "brand"), work(20, "M", "brand"),
    work(21, "M", "brand"), work(22, "M", "brand"), work(23, "OFF", "neutral"),
    work(24, "M", "brand"),
    work(25, "M", "brand"), work(26, "M", "brand"), work(27, "A", "ok"),
    // The one day this person also holds the duty. Drawn as a marker ON the
    // shift, because MOD is a duty a person holds while working their own
    // posting — WF-Q1, never a replacement for it.
    { date: 28, mark: "M", tone: "brand", duty: true },
    work(29, "M", "brand"), work(30, "OFF", "neutral"), work(31, "M", "brand"),
  ],
};
