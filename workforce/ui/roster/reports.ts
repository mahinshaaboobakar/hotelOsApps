/**
 * What the month-end summary is given.
 *
 * # These are inputs, not a payslip
 *
 * Workforce produces the numbers and never calculates pay. Every column is
 * traceable to a fact the application already holds, and **nothing on the
 * screen can be edited** — a number you can type over is not a record.
 */

/** One person's month. */
export interface MonthRow {
  who: string;
  role: string;
  posted: number;
  present: number;
  late: number;
  casual: number;
  sick: number;
  earned: number;
  comp: number;

  /**
   * Holidays worked — **or null, never zero**.
   *
   * `WF-Q18`: the figure needs a property holiday calendar, which Core
   * Administration owns and which does not exist yet. A zero would be
   * indistinguishable from a property whose staff worked no holidays, and
   * payroll would have no way to know the number was never computed.
   */
  holidays: number | null;

  /** Hours worked in the period. */
  hours: number;

  /**
   * Overtime hours, zero included.
   *
   * It used to arrive as a string, and as the magic value `"0"` rather than
   * `"0.0"` when there was none — so the screen's own `=== "0"` test, which
   * decides whether the figure is drawn quietly, depended on the exact
   * characters a service chose.
   */
  overtime: number;
}

/** The month. */
export interface Month {
  /**
   * The month the report covers, as `YYYY-MM-DD` on its first day.
   *
   * The header sentence was built in the service — *"September 2026 · Front
   * Office · 01 Sep – 30 Sep"* — so a month name, two day formats, a range
   * separator and the middle dots joining them were all one culture's. Three of
   * those five are the reader's and the other two are punctuation a reader's
   * script may not use (ADR 0175).
   */
  month: string;

  /** The first day the report covers. */
  from: string;

  /** The last. */
  to: string;
  /** The department asked for, echoed by the service — null, since this screen names none. */
  department: string | null;
  rows: readonly MonthRow[];
}

export const recordedMonth: Month = {
  month: "2026-08-01",
  from: "2026-08-01",
  to: "2026-08-31",
  department: null,
  rows: [
    {
      who: "Priya Thomas", role: "Supervisor", posted: 26, present: 26, late: 1,
      casual: 0, sick: 0, earned: 0, comp: 0, holidays: null,
      hours: 208.5, overtime: 6.5,
    },
    {
      who: "Anjali Menon", role: "Receptionist", posted: 24, present: 22, late: 4,
      casual: 2, sick: 0, earned: 0, comp: 0, holidays: null,
      hours: 176.0, overtime: 0,
    },
    {
      who: "Vishnu Das", role: "Night auditor", posted: 26, present: 26, late: 0,
      casual: 0, sick: 0, earned: 0, comp: 1, holidays: null,
      hours: 212.0, overtime: 10.0,
    },
    {
      who: "Sneha Iyer", role: "Receptionist", posted: 23, present: 21, late: 3,
      casual: 0, sick: 2, earned: 0, comp: 0, holidays: null,
      hours: 168.0, overtime: 0,
    },
    {
      who: "Joseph Kurian", role: "Bell captain", posted: 25, present: 25, late: 0,
      casual: 0, sick: 0, earned: 5, comp: 0, holidays: null,
      hours: 200.0, overtime: 2.0,
    },
    {
      who: "Rani Rajan", role: "Guest relations", posted: 24, present: 23, late: 2,
      casual: 1, sick: 0, earned: 0, comp: 0, holidays: null,
      hours: 184.0, overtime: 0,
    },
  ],
};
