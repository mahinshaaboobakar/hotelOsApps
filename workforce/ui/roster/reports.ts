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

  hours: string;
  overtime: string;
}

/** The month. */
export interface Month {
  label: string;
  department: string;
  rows: readonly MonthRow[];
}

export const recordedMonth: Month = {
  label: "August 2026 · Front Office · business days 1–31",
  department: "Front Office",
  rows: [
    {
      who: "Priya Thomas", role: "Supervisor", posted: 26, present: 26, late: 1,
      casual: 0, sick: 0, earned: 0, comp: 0, holidays: null,
      hours: "208.5", overtime: "6.5",
    },
    {
      who: "Anjali Menon", role: "Receptionist", posted: 24, present: 22, late: 4,
      casual: 2, sick: 0, earned: 0, comp: 0, holidays: null,
      hours: "176.0", overtime: "0",
    },
    {
      who: "Vishnu Das", role: "Night auditor", posted: 26, present: 26, late: 0,
      casual: 0, sick: 0, earned: 0, comp: 1, holidays: null,
      hours: "212.0", overtime: "10.0",
    },
    {
      who: "Sneha Iyer", role: "Receptionist", posted: 23, present: 21, late: 3,
      casual: 0, sick: 2, earned: 0, comp: 0, holidays: null,
      hours: "168.0", overtime: "0",
    },
    {
      who: "Joseph Kurian", role: "Bell captain", posted: 25, present: 25, late: 0,
      casual: 0, sick: 0, earned: 5, comp: 0, holidays: null,
      hours: "200.0", overtime: "2.0",
    },
    {
      who: "Rani Rajan", role: "Guest relations", posted: 24, present: 23, late: 2,
      casual: 1, sick: 0, earned: 0, comp: 0, holidays: null,
      hours: "184.0", overtime: "0",
    },
  ],
};
