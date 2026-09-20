/**
 * What the Leave & Requests screens are given — shapes, and the frames' facts.
 *
 * One file for both tabs because they are one screen: *Requests* is what this
 * person asked for, *Approvals* is what is waiting on them, and the approver is
 * the same person the posting resolves either way.
 */

/** A balance, as the sheet where the decision is made shows it. */
export interface Balance {
  /**
   * The leave type's id — what a request is raised against.
   *
   * The name is what a person reads and is not a key. `raise` takes this, so
   * the read that draws the balances is the one place the dialog can learn it.
   */
  id: string;

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

  /**
   * How much accrues each month, or null where HR grants it by hand.
   *
   * This was `note: string`, carrying `"accrues 2 / month"` — a sentence built
   * in a service, with an English verb, a unit and a locale's decimal mark in
   * it. The surface composes it now, which is the only place that knows how a
   * reader says any of the three.
   */
  accruesPerMonth: number | null;
}

/** Where a request has got to. */
export type RequestState = "Requested" | "Approved" | "Declined" | "Cancelled";

/** One leave request, as this person's list shows it. */
export interface LeaveRow {
  /** The request, for a write that names it — `leave.request · withdraw`. */
  id: string;

  /** The version that write expects. */
  version: number;

  type: string;

  /**
   * What the person wrote with the request, or null when they wrote nothing.
   *
   * **Null, not "—".** The service sent a dash for an empty note and this
   * screen tested for it — `row.note !== "—"` — so a punctuation choice in the
   * service decided what was drawn here. An absence is an absence; the dash,
   * if there is to be one, is the screen's.
   */
  note: string | null;
  /**
   * The two ends of the leave, as the wire carries them - ADR 0175.
   *
   * The service used to join them, in its own culture, and compress a
   * same-month range to "7 - 8 Sep". That compression assumes the month
   * FOLLOWS the day, which is true of en-GB and false of en-US, so it was a
   * locale's word order written into a service.
   */
  dates: { from: string; to: string };
  days: number;
  state: RequestState;
}

/** One item waiting on the approver — a leave request or a swap proposal. */
export interface Waiting {
  /**
   * The request or proposal this row is, for a write that names one.
   *
   * Approve, decline and withdraw each take an id and the version they expect.
   * The read carried neither, so the queue could show rows and offer no
   * decision on any of them — the owner's ruled panel (`64g` §4 B) could not
   * have been built whatever it drew.
   */
  id: string;

  /** The version those writes expect. */
  version: number;

  /** Who it concerns. */
  who: string;

  /** A swap row's own words. Absent on a leave row, which sends values instead. */
  what?: string;

  /**
   * A leave row's type and length — the screen writes "Casual · 3 days".
   *
   * The service sent that sentence whole, in its own culture (NUM-Q1, ADR
   * 0174). `type` is null for a type no longer in the catalogue.
   */
  type?: string | null;
  days?: number;

  /** Which of the two kinds. */
  kind: "Leave" | "Swap";

  /** When. */
  /**
   * The two ends of the leave, for a row that is a leave request.
   *
   * **Absent on a swap row**, which has an acceptance day instead. The two
   * arrived under one name while both were rendered strings, so a row's shape
   * was invisible: `"7 - 8 Sep"` and `"27 Aug"` are the same type and mean
   * different things.
   */
  dates?: { from: string; to: string };

  /** When the colleague accepted - a swap row's day. Absent on a leave row. */
  accepted?: string | null;

  /**
   * What this person holds of that type now, and what approving would leave.
   *
   * Both may be negative: an approved overdraw is a real state (`WF-Q5`), and
   * a number clamped at zero would hide the decision somebody made. **Null
   * when nothing has ever been posted** for that person and type — a zero
   * would claim a ledger row exists. Absent on a swap row.
   */
  balance?: number | null;
  after?: number | null;

  /** What the person wrote with the request, or null. Absent on a swap row. */
  note?: string | null;

  /**
   * Who else is already away across these dates — the fact an approver is
   * actually weighing. Never includes this request's own person.
   */
  alsoOff?: readonly string[];
}

/**
 * The swap the approver has open — its three steps, and both cells.
 *
 * **Nothing produces one yet.** `LeaveView.Board` sends `swap: null` on every
 * call, because no proposal is open until somebody picks one and nothing on
 * the wire picks one. This is the shape the card draws when one arrives; it is
 * not evidence that one does.
 */
export interface SwapDetail {
  /** The day both cells change, as an ISO date — the screen writes it (ADR 0175). */
  on: string;
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

  /** The open proposal — null, which is what the service sends every time. */
  swap: SwapDetail | null;
}

export const recordedLeave: LeaveBoard = {
  balances: [
    {
      id: "3f1c0a64-5d21-4e8b-9f02-1a7c6b40d911", type: "Casual",
      days: 4, of: 8, accruesPerMonth: 2,
    },
    {
      id: "7a2e5b18-9c43-4d6f-8b10-2e5d9c7a4f36", type: "Sick",
      days: 6, of: 12, accruesPerMonth: null,
    },
    // The frame's own minus sign. A screen that clamped this at zero would be
    // hiding the decision its manager already made.
    {
      id: "c58d3712-6b90-4a2e-97d4-8f3b1e6c05a7", type: "Earned",
      days: -1, of: 15, accruesPerMonth: null,
    },
    {
      id: "e94b7f25-1a38-4c6d-b052-7d9e3f8a16c4", type: "Comp-off",
      days: 2, of: null, accruesPerMonth: null,
    },
  ],

  requests: [
    {
      id: "2f4c8a61-3d97-4e52-b8a0-6c1f9d3e7b25", version: 1,
      type: "Casual leave", note: "Family function — will be back Monday",
      dates: { from: "2026-09-07", to: "2026-09-08" }, days: 2, state: "Requested",
    },
    {
      id: "9b3e7d40-5a12-4f86-9c37-1e8b4a2d6f09", version: 2,
      type: "Earned leave", note: "Approved with the balance overdrawn by 1",
      dates: { from: "2026-08-18", to: "2026-08-22" }, days: 5, state: "Approved",
    },
    // A request with nothing written on it: null, as the wire now carries it.
    // This was "—", the service's own punctuation, which the screen then
    // tested for.
    {
      id: "6d1a9c58-7b24-4e30-85f1-3a7c2e9b4d16", version: 2,
      type: "Sick leave", note: null,
      dates: { from: "2026-08-03", to: "2026-08-03" }, days: 1, state: "Approved",
    },
    {
      id: "c47f2b83-8e15-4a69-b3d2-5f0c7a1e9438", version: 3,
      type: "Casual leave",
      note: "Withdrawn before the decision — the balance was credited back",
      dates: { from: "2026-07-11", to: "2026-07-11" }, days: 1, state: "Cancelled",
    },
  ],

  // The wire's shape (LeaveView.Queue): a swap row's fixed words, and a leave
  // row's type and days as values. These carried "accepted by Sneha" (the
  // service names nobody there) and "Earned · 4 days · balance 1 of 15" — a
  // balance the queue has never sent.
  waiting: [
    {
      id: "a15e3c72-4b68-4d91-8f05-2c9a7e1b6d34", version: 2,
      who: "Anjali Menon & Sneha Iyer",
      what: "Swap — accepted, awaiting you",
      kind: "Swap", accepted: "2026-08-27",
    },
    {
      id: "5c82f4a9-7e31-4b06-9d58-1f3e6c4a8b27", version: 1,
      who: "Joseph Kurian", type: "Casual", days: 2, kind: "Leave",
      dates: { from: "2026-09-07", to: "2026-09-08" },
      // What the decision would leave behind, and who else is away then.
      balance: 11, after: 9, note: "Family function",
      alsoOff: ["Sneha Iyer"],
    },
    {
      // Nothing posted for this person and type: the panel says so rather
      // than drawing a zero, which would claim a ledger row exists.
      id: "e37b6d05-2a94-4f18-b7c3-9d05e2a1c846", version: 1,
      who: "Rani Rajan", type: "Earned", days: 4, kind: "Leave",
      dates: { from: "2026-09-12", to: "2026-09-15" },
      balance: null, after: null, note: null, alsoOff: [],
    },
  ],

  // What the service sends (`LeaveView.cs`, `swap = null`). This carried a whole
  // open proposal the wire has never produced, so the Approvals tab rendered
  // only here and threw on every real property.
  swap: null,
};
