/**
 * One stay, in depth — the panel, the banner and the timeline. Frame 3.
 */

import type { Mark, Tag } from "./day";
/** One label–value row of THE STAY panel. */
export interface DetailRow {
  label: string;

  /** Rendered before any emphasis; may be empty when `strong` carries it all. */
  value: string;

  /** The part the design sets bold — a room number, a time. */
  strong?: string;

  /** Trailing text after the strong part, e.g. ` · Deluxe King`. */
  tail?: string;

  /** Italic-muted, for a preference or a note. */
  quiet?: boolean;

  tags: readonly Tag[];
}

/** Which value a person kept — GUEST-Q3 (2). */
export type ClearSide = "ours" | "pms";

/** The amber band: a disagreement standing over an override. */
export interface Banner {
  headline: string;
  detail: string;
  attribution: string;

  /**
   * The row being decided.
   *
   * One stay can hold disagreements about different aspects, so the clear names
   * the row rather than the stay — and the screen cannot derive it.
   */
  disagreementId: string;

  /**
   * The first is the primary. Both values stay on the record either way.
   *
   * **`side` is a fact, not a restatement of the label.** Until 2026-10-04
   * these were bare strings, so settling the disagreement would have meant
   * deciding which side a button meant by reading its own English — a control
   * that breaks the day somebody rewords the caption.
   */
  actions: readonly { label: string; side: ClearSide }[];
}

/** One entry of the activity timeline. */
export interface Moment {
  time: string;
  tone: Mark | "none";
  what: string;
  detail: string;
}

/** A tab of the stay page, with its count where the design shows one. */
export interface Tab {
  label: string;
  count?: string;

  /**
   * Dimmed, because the application whose subject it shows is not installed.
   *
   * **Dimmed, not removed.** Which tabs a stay has is itself information: a
   * property looking at a greyed Servicing learns that servicing is something
   * HotelOS can show them, and an absent tab teaches them nothing.
   *
   * This was written by `screens/stay` and read by nobody for the length of one
   * edit — `Tab` had no such field, `tabs()` ignored it, and **the typecheck
   * passed**, because a `{ ...tab, gone: true }` spread is not a fresh object
   * literal and so escapes excess-property checking. A dead feature behind a
   * green build; `tests/tabs.test.ts` is what can now fail on it.
   */
  gone?: boolean;
}

/** The stay page — gold frame 3. */
export interface StayPage {
  id: string;
  guest: string;
  room: string | null;
  stayId: string;
  bookingRef: string;

  /**
   * The version this page was read at.
   *
   * **Every write this screen offers is made against it** — assign, move,
   * check out, cancel. The page carried none until 2026-09-23, so the check-in
   * borrowed one from the registration card: a screen whose actions write and
   * whose read has no version cannot make the concurrency check mean anything.
   */
  version: number;

  /** The type, for the room chooser. The room NUMBER is what a person reads. */
  roomTypeId: string;

  /** The room it has now — null is a stay waiting for one. */
  currentRoomId: string | null;

  /**
   * The booking this stay belongs to.
   *
   * **`bookingRef` beside it is what a PERSON reads** — an external reference,
   * or `created here` — and cannot be sent to a service. Cancelling is the
   * booking's operation (GUEST-Q2), so the stay screen's `Cancel…` needs the
   * id rather than the reference.
   */
  bookingId: string;

  /** `Opera manages this stay`, or null in a standalone property. */
  managedBy: string | null;

  actions: readonly { label: string; danger: boolean }[];
  tabs: readonly Tab[];

  banner: Banner | null;

  /** `override standing`, shown on the panel header. */
  standing: string | null;

  rows: readonly DetailRow[];
  timeline: readonly Moment[];

  /** The sentence under the timeline explaining what taking Opera's value does. */
  consequence: string;
}

/**
 * What recording a no-show would do — the owner's N1 dialog, ruled 2026-09-24.
 *
 * **A read.** It names a forfeit and a consequence and writes nothing, so a
 * person who may look at a stay may see what the action would do; only the
 * button needs `stay.override`.
 */
export interface NoShowPlan {
  stayId: string;

  /** The version the dialog was read at, which the confirm carries back. */
  version: number;

  /** The primary guest, or `Not yet named` — a state, not a placeholder. */
  guest: string;

  /** The booking reference, where a source manages this stay. */
  reference: string | null;

  /** ISO instants. The screen compresses the range in the reader's grammar. */
  arrive: string | null;
  depart: string | null;

  /**
   * What the terms say is forfeited.
   *
   * **Three states, and each is a different fact.** `null` is *no terms are
   * stored*, `{ unstated }` is *terms with no currency* — a number a guest
   * could be charged in the wrong denomination — and the third is an amount.
   * A zero would be a forfeit of nothing, which none of them means.
   */
  forfeit: null | { unstated: string } | Forfeit;

  /** What becomes of the stay, in the service's own words. */
  afterwards: string;
}

/** A forfeit, in the parts the screen composes — ADR 0175 §NUM-Q2. */
export interface Forfeit {
  /** A decimal string, invariant. Never pre-formatted. */
  amount: string;

  /** ISO 4217 alphabetic. The screen decides the symbol and where it goes. */
  currency: string;

  /** `Gross` or `Net` — whether the amount includes tax. */
  basis: string;

  /** Null where the terms named none; the screen then omits the count. */
  nights: number | null;
}

/**
 * What correcting a lifecycle fact would do — the owner's C1 dialog.
 *
 * **The words are the screen's.** ADR 0175: this carries where the stay would
 * go and what stands in the way, and the title, the button and the prose are
 * composed where they are rendered.
 */
export interface CorrectPlan {
  stayId: string;

  /** The version the dialog was read at, which the confirm carries back. */
  version: number;

  /** The state the confirm sends. Null where no correction is defined. */
  to: string | null;

  /** Where the stay is now. */
  from: string;

  /** The number the desk knows the room by, where one is recorded. */
  room: string | null;

  /** The primary guest, or `Not yet named` — a state, not a placeholder. */
  guest: string;

  /** When the departure was recorded, as an ISO instant. */
  departedAt: string | null;

  /**
   * When the no-show was recorded, as an ISO instant. Null for a departure.
   *
   * **From the event store, which is where the fact is.** `RecordNoShowAsync`
   * writes no such column and skips its override row for a stay the PMS has
   * never seen, so the stay row cannot answer this — and the Activity tab
   * already reads the same `stay.no_show` event, so a dialog reading it too
   * cannot disagree with the list behind it.
   */
  noShowAt: string | null;

  /** The booking's reference, where the source gave one. */
  reference: string | null;

  /** The booked arrival, ISO `yyyy-MM-dd`. */
  arrive: string | null;

  /** The booked departure, ISO `yyyy-MM-dd`. */
  depart: string | null;

  /**
   * Whether the room is still nobody else's.
   *
   * **Null is not a missing boolean** — it means no room is in the question,
   * which is what reinstating a no-show is. Reading it as *taken* would tell
   * the desk a room was held when there is no room.
   */
  roomStillFree: boolean | null;

  /**
   * The reasons the property configured — **and nothing configures them**.
   *
   * Empty, exactly as `CancelPlan.reasons` is empty and for the same recorded
   * reason: `GuestOpsSettings` carries no reason vocabulary and frame 16
   * configures none. The screen draws the field with nothing in it and the
   * button unavailable, rather than a hardcoded list nobody chose.
   */
  reasons: readonly string[];
}
