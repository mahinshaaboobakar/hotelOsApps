/**
 * What the Leave & Requests screens are given — shapes, and the frames' facts.
 *
 * One file for both tabs because they are one screen: *Requests* is what this
 * person asked for, *Approvals* is what is waiting on them, and the approver is
 * the same person the posting resolves either way.
 */

/** A balance, as the sheet where the decision is made shows it. */
export interface Balance {
  /** The leave type's name. */
  type: string;

  /**
   * Days remaining. **May be negative** — an approved overdraw is a real state
   * under `WF-Q5`, not an error, so every surface showing one must survive a
   * minus sign.
   */
  days: number;

  /** The whole entitlement, when the type accrues one. */
  of: number | null;

  /** How it accrues, or how it is granted. */
  note: string;
}

/** Where a request has got to. */
export type RequestState = "Requested" | "Approved" | "Declined" | "Cancelled";

/** One leave request, as this person's list shows it. */
export interface LeaveRow {
  type: string;
  note: string;
  dates: string;
  days: number;
  state: RequestState;
}

/** One item waiting on the approver — a leave request or a swap proposal. */
export interface Waiting {
  /** Who it concerns. */
  who: string;

  /** What it is, in the queue's own words. */
  what: string;

  /** Which of the two kinds. */
  kind: "Leave" | "Swap";

  /** When. */
  dates: string;
}

/** The swap the approver has open — its three steps, and both cells. */
export interface SwapDetail {
  when: string;
  proposer: string;
  colleague: string;

  /** Each person's posting, because a swap is between two postings. */
  proposerWhere: string;
  colleagueWhere: string;

  /** The proposer's shift before and after. */
  proposerShifts: readonly [string, string];

  /** The colleague's, the other way round. */
  colleagueShifts: readonly [string, string];

  /**
   * Who did what, and when — on the card, not in an audit screen.
   *
   * `WF-Q9`(b)'s provenance obligation where a person can actually see it.
   */
  provenance: string;
}

/** Everything the Leave & Requests screen draws. */
export interface LeaveBoard {
  balances: readonly Balance[];
  requests: readonly LeaveRow[];
  waiting: readonly Waiting[];
  swap: SwapDetail;
}

export const recordedLeave: LeaveBoard = {
  balances: [
    { type: "Casual", days: 4, of: 8, note: "accrues 2 / month" },
    { type: "Sick", days: 6, of: 12, note: "" },
    // The frame's own minus sign. A screen that clamped this at zero would be
    // hiding the decision its manager already made.
    { type: "Earned", days: -1, of: 15, note: "approved overdraw" },
    { type: "Comp-off", days: 2, of: null, note: "granted by HR" },
  ],

  requests: [
    {
      type: "Casual leave", note: "Family function — will be back Monday",
      dates: "7 – 8 Sep", days: 2, state: "Requested",
    },
    {
      type: "Earned leave", note: "Approved with the balance overdrawn by 1",
      dates: "18 – 22 Aug", days: 5, state: "Approved",
    },
    { type: "Sick leave", note: "—", dates: "3 Aug", days: 1, state: "Approved" },
    {
      type: "Casual leave",
      note: "Withdrawn before the decision — the balance was credited back",
      dates: "11 Jul", days: 1, state: "Cancelled",
    },
  ],

  waiting: [
    {
      who: "Anjali Menon & Sneha Iyer",
      what: "Swap — accepted by Sneha, awaiting you",
      kind: "Swap", dates: "27 Aug",
    },
    { who: "Joseph Kurian", what: "Casual · 2 days", kind: "Leave", dates: "7–8 Sep" },
    {
      who: "Rani Rajan", what: "Earned · 4 days · balance 1 of 15",
      kind: "Leave", dates: "12–15 Sep",
    },
  ],

  swap: {
    when: "Thursday 27 August",
    proposer: "Anjali Menon",
    colleague: "Sneha Iyer",
    proposerWhere: "Receptionist · Zone 3",
    colleagueWhere: "Receptionist · Zone 2",
    proposerShifts: ["A", "M"],
    colleagueShifts: ["M", "A"],
    provenance:
      "Anjali proposed it on 24 Aug, 18:40, from My Schedule. "
      + "Sneha accepted on 24 Aug, 19:02. Your approval commits both cells at once.",
  },
};
