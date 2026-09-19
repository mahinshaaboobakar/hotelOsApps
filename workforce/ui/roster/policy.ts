/**
 * What the Policy screen is given — the four things a property configures,
 * and the one it does not.
 */

import type { Span } from "../chrome/clock";

/** A shift in the property's catalogue. */
export interface CatalogueRow {
  name: string;
  code: string;
  /** The working hours, as two ends — null on an off day. */
  hours: Span | null;

  /** A split shift's second window, or null. */
  second: Span | null;
  colour: string;

  /**
   * The published tone the colour is drawn in — the SERVICE's mapping
   * (`Wording.Tone`), the one the rota and the schedule already draw.
   *
   * Sent rather than mapped here: Shifts and Policy each kept a `swatch()` that
   * drew Rose neutral and Violet brand while the service made them bad and
   * neutral, so one shift was two colours on two screens (ledger D3).
   */
  tone: "brand" | "ok" | "warn" | "bad" | "neutral";
  kind: "working" | "off";

  /** How many assignments reference it — the reason a retire is not a delete. */
  inUse: string;
}

/** A leave type and how it accrues. */
export interface LeaveRow {
  type: string;
  /**
   * How much accrues each month, or null where HR grants it by hand.
   *
   * A number, because the decimal mark, the unit and the word for *month* are
   * all the reader's. It arrived as `"accrues 2 / month"` — an English sentence
   * built in a service, which a surface could not have said any other way.
   */
  accruesPerMonth: number | null;

  /** The whole year's entitlement, or null where the type does not accrue. */
  perYear: number | null;
  note: string;
}

/** The screen. */
export interface Policy {
  property: string;
  catalogue: readonly CatalogueRow[];
  leave: readonly LeaveRow[];
  /** The daily overtime threshold in hours, or null where none is set. */
  overtimeDaily: number | null;

  /** The weekly one. */
  overtimeWeekly: number | null;

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
    {
      name: "Morning", code: "M",
      hours: { from: "07:00", to: "15:00" }, second: null,
      colour: "Cyan", tone: "brand", kind: "working", inUse: "412 assignments"
    },
    {
      name: "Afternoon", code: "A",
      hours: { from: "15:00", to: "23:00" }, second: null,
      colour: "Indigo", tone: "brand", kind: "working", inUse: "380 assignments"
    },
    {
      name: "Night", code: "N",
      hours: { from: "23:00", to: "07:00" }, second: null,
      // Neutral: the service's table has no Violet, and the fixture carries
      // what the wire would, not what the old swatch() drew (brand).
      colour: "Violet", tone: "neutral", kind: "working", inUse: "196 assignments"
    },
    {
      name: "Split — Banquet", code: "SB",
      hours: { from: "10:00", to: "14:00" },
      second: { from: "18:00", to: "22:00" },
      colour: "Amber", tone: "warn", kind: "working", inUse: "44 assignments"
    },
    {
      name: "General", code: "G",
      hours: { from: "09:00", to: "18:00" }, second: null,
      colour: "Emerald", tone: "ok", kind: "working", inUse: "88 assignments"
    },
    {
      name: "Week-off", code: "OFF", hours: null, second: null,
      colour: "None", tone: "neutral", kind: "off", inUse: "203 assignments"
    },
  ],

  leave: [
    { type: "Casual", accruesPerMonth: 2, perYear: 24, note: "—" },
    { type: "Sick", accruesPerMonth: 1, perYear: 12, note: "—" },
    { type: "Earned", accruesPerMonth: 1.25, perYear: 15, note: "—" },
    // No accrual row, because HR grants it — WF-Q13. Null would be wrong here:
    // the property configured a type that is granted, which is a decision.
    {
      type: "Comp-off", accruesPerMonth: null, perYear: null,
      note: "Holidays worked are counted; HR grants the credit",
    },
  ],

  overtimeDaily: 9,
  overtimeWeekly: 48,

  // What the service sends: PolicyView answers holidays = null, because the
  // property's holiday calendar has no owner yet. This carried "14 declared
  // holidays this year — 26 Jan, 1 May, 15 Aug, 2 Oct, Onam (4 days), Diwali
  // (2), Christmas…" — a list nobody declared, and one country's calendar in a
  // product sold into India and the GCC — so every capture showed it and no
  // property ever could.
  holidays: null,
};
