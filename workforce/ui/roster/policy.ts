/**
 * What the Policy screen is given — the four things a property configures,
 * and the one it does not.
 */

/** A shift in the property's catalogue. */
export interface CatalogueRow {
  name: string;
  code: string;
  times: string;
  colour: string;
  kind: "working" | "off";

  /** How many assignments reference it — the reason a retire is not a delete. */
  inUse: string;
}

/** A leave type and how it accrues. */
export interface LeaveRow {
  type: string;
  accrues: string;
  perYear: string;
  note: string;
}

/** The screen. */
export interface Policy {
  property: string;
  catalogue: readonly CatalogueRow[];
  leave: readonly LeaveRow[];
  overtimeDaily: string;
  overtimeWeekly: string;

  /**
   * The holiday calendar — **read-only, and not this application's**.
   *
   * `WF-Q16`: the administrator establishes it in Core Administration exactly
   * as they establish check-in time. Workforce reads it and does not own it,
   * which is why this is a sentence rather than an editable table.
   */
  holidays: string | null;
}

export const recordedPolicy: Policy = {
  property: "Kochi Beach Resort · applies to every department",

  catalogue: [
    { name: "Morning", code: "M", times: "07:00 – 15:00", colour: "Cyan", kind: "working", inUse: "412 assignments" },
    { name: "Afternoon", code: "A", times: "15:00 – 23:00", colour: "Indigo", kind: "working", inUse: "380 assignments" },
    { name: "Night", code: "N", times: "23:00 – 07:00", colour: "Violet", kind: "working", inUse: "196 assignments" },
    { name: "Split — Banquet", code: "SB", times: "10–14, 18–22", colour: "Amber", kind: "working", inUse: "44 assignments" },
    { name: "General", code: "G", times: "09:00 – 18:00", colour: "Emerald", kind: "working", inUse: "88 assignments" },
    { name: "Week-off", code: "OFF", times: "—", colour: "None", kind: "off", inUse: "203 assignments" },
  ],

  leave: [
    { type: "Casual", accrues: "2 / month", perYear: "24", note: "—" },
    { type: "Sick", accrues: "1 / month", perYear: "12", note: "—" },
    { type: "Earned", accrues: "1.25 / month", perYear: "15", note: "—" },
    // No accrual row, because HR grants it — WF-Q13. Null would be wrong here:
    // the property configured a type that is granted, which is a decision.
    {
      type: "Comp-off", accrues: "granted by HR", perYear: "—",
      note: "Holidays worked are counted; HR grants the credit",
    },
  ],

  overtimeDaily: "9 h / day",
  overtimeWeekly: "48 h / week",

  // What Core Administration establishes. Shown so a manager knows the rota
  // plans around it — and shown as text, because this screen cannot edit it.
  holidays:
    "14 declared holidays this year — 26 Jan, 1 May, 15 Aug, 2 Oct, "
    + "Onam (4 days), Diwali (2), Christmas…",
};
