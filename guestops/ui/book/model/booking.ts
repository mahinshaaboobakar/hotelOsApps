/**
 * The booking lifecycle — the list, one booking, and cancelling one. Frames 2, 8, 9.
 */

import type { Chip, Tag } from "./day";

/**
 * The four tones a stay's status takes — frame 2 uses all of them.
 *
 * `bad` is Cancelled and No-show, and it is a tone rather than a removal:
 * both rows stay in the list, because a cancelled reservation exists and a
 * no-show is reportable (S25, S27, ADR 0062).
 */
export type StatusTone = "ok" | "warn" | "neutral" | "bad";

/**
 * One row of the Bookings list — frame 2.
 *
 * A **booking**, not a stay: the list is what the property has sold, and the
 * row's `rooms` is how many stays are inside it. Frame 2's first row reads
 * `1 of 3 known`, which is the incomplete group said out loud rather than
 * three rows invented to make the count come out right (GUEST-Q2, frame 9).
 */
export interface BookingRow {
  id: string;
  guest: string;

  /**
   * Absent, and reported as absent — GUEST-Q12.
   *
   * The drawing shows `+91 98470 •••• 12`. Contacts are stored encrypted and
   * the protector has only a write direction, so nothing in this process can
   * produce a masked form of a value it cannot read. Null renders as nothing,
   * never as a plausible number.
   */
  contact: string | null;

  /** True for a party member the source has not named yet, drawn italic. */
  unnamed: boolean;

  /** The source's reference, or the design's own `created here`. */
  reference: string;

  /** True when `reference` is the phrase rather than a real reference. */
  createdHere: boolean;

  /**
   * The number the guest reads off their email — frame 2's `84119377`.
   *
   * Separate from `reference` because they are different identifiers from
   * different systems: the booking reference is what the property calls the
   * booking, the confirmation number is what the source gave the guest. The
   * list is where a guest at the counter is found, so it must be searchable by
   * either, and printing one when asked for the other would send a receptionist
   * looking for a booking nobody can quote.
   */
  confirmation: string | null;

  /** `1`, or `1 of 3 known` when the source claimed more than it has sent. */
  rooms: string;

  /** `31 Aug → 2 Sep`. */
  dates: string;

  /** `In house`, `Booked`, `Cancelled` — the stay lifecycle, aggregated. */
  status: string;

  /** Which tone the status pill takes. */
  statusTone: StatusTone;

  /** Where the booking came from and what disagrees about it. */
  chips: readonly Chip[];
}

/** One choice in a filter — the label, and whether it is the one showing. */
export interface Choice {
  label: string;
  on: boolean;
}

/** One page of the Bookings list, and what the filters were set to. */
export interface Bookings {
  /** What the search box shows. Empty is the ordinary state. */
  search: string;

  /** Arrival window, status and source, in the drawing's order. */
  filters: readonly { key: string; choices: readonly Choice[] }[];

  total: number;
  rows: readonly BookingRow[];
}

/** One stay inside a booking — frames 8 and 9. */
export interface BookingStay {
  id: string;
  guest: string;
  unnamed: boolean;

  /** `01J9M…22B1` — the stay's own id, elided the way the drawing elides it. */
  stayId: string;

  roomType: string;

  /**
   * The room, where one is assigned.
   *
   * Frame 8 draws no room column and frame 9 draws no stay column, on what is
   * one screen. **Reported as a frame-to-frame divergence and built as the
   * union**: a receptionist looking at a booking wants the room, and the
   * cancellation dialog needs to name individual stays. Neither frame loses
   * anything it drew; each shows one more column than it happened to include.
   */
  room: string | null;

  dates: string;
  status: string;
  statusTone: StatusTone;
  chips: readonly Chip[];
}

/** One booking and its stays — the page frames 8 and 9 both draw. */
export interface BookingDetail {
  id: string;
  guest: string;
  reference: string;

  /** `Two stays · 3 Sep → 7 Sep`. */
  summary: string;

  /** `Opera manages this booking`, or null in a standalone property. */
  managedBy: string | null;

  stays: readonly BookingStay[];

  /**
   * What the source claimed and has not sent — frame 9.
   *
   * Null when the booking is complete. When it is not, the missing stays are
   * **not rows**: an unsent stay has no room type, no dates and no guest, and
   * a placeholder row would be a stay nobody booked.
   *
   * The reference system met this exact shape — three rooms claimed, one room
   * described — and answered it by minting sibling identifiers by string
   * concatenation that always produced `-1` (R9). **Two grey placeholder rows
   * are the same mistake in a nicer font**: they invent stays the source never
   * sent, and every count downstream inherits them.
   */
  incomplete: string | null;

  /**
   * The same fact, said again where the design says it twice.
   *
   * Frame 9 opens with a banner and repeats it under the table as a note,
   * because the two answer different questions — *what am I looking at* and
   * *why is there one row*. Null when the booking is complete.
   */
  incompleteDetail: string | null;

  /**
   * Other properties this group has legs at — frame 9.
   *
   * **Sayable, not queryable** (S4, S32). A group identifier is carried from
   * the first fact, so a chain-level journey needs no migration later; what is
   * deliberately not built is a cross-installation query. This installation
   * holds only its own stays and asks nobody for the rest, and the sentence
   * says exactly that rather than implying a total.
   */
  elsewhere: string | null;

  /**
   * The three cards under the table — frame 9.
   *
   * Each is a fact about how a *group* behaves rather than about this booking:
   * check-in is per stay, the identifier is carried from day one, and an
   * expected count that is stated is a different thing from one that is not.
   */
  facts: readonly GroupFact[];
}

/** One card of frame 9's three. */
export interface GroupFact {
  title: string;
  key: string;
  value: string;
  hint: string;
}

/**
 * What cancelling this booking will actually do — frame 8's dialog.
 *
 * Computed and returned as a **plan**, before anything is written. The
 * confirmation names the object, the consequence and the limit (ADR 0106 §3),
 * and every one of those three is a fact the server has to supply: the desk
 * cannot be asked to confirm a penalty the screen invented.
 */
export interface CancelPlan {
  /** `BK-4506 · Fatima Sheikh · two stays, 3 – 7 September`. */
  subject: string;

  /** `This cancels two stays, one at a time.` */
  consequence: string;

  /**
   * How many stays the button will cancel.
   *
   * **Stated, never counted off `rows`.** The rows are heterogeneous — a
   * penalty per stay, then why there is one, then what happens afterwards — so
   * a count taken from their length is right only for as long as the trailing
   * rows stay the same number. It was wrong the first time it was drawn: a
   * two-stay booking offered *Cancel all 3 stays*, because the arithmetic had
   * assumed one trailing row and the plan had two.
   */
  stays: number;

  /** One row per stay, plus the why and the afterwards. */
  rows: readonly { label: string; value: string; strong?: string; tags: readonly Tag[] }[];

  /**
   * The sentence that must not be omitted — frame 8, `CONN-Q5`.
   *
   * Null in a standalone property, where there is no PMS to fail to tell. In a
   * connected one it is the limit of what the button does, and a cancellation
   * screen that stayed silent about it would let a receptionist believe the
   * room had been released in Opera.
   */
  notTold: string | null;

  /** The reasons the property configured. The first is the one showing. */
  reasons: readonly string[];
}
