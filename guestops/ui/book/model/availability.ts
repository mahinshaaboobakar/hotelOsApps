/**
 * What is free to sell, and the walk-in that is sold at the desk. Frames 14, 10.
 */

/**
 * One room type's answer for the requested dates — frame 14.
 *
 * **Computed, never a table anyone feeds** (GUEST-Q7). Four numbers from three
 * owners: `total` is Master Data's, `sold` is ours, `outOfOrder` is
 * EngineeringOps's heard as an event, and `stopSold` is ours. `free` is what
 * remains, and it is the only one that is not somebody's stored fact.
 *
 * The two attributions exist because the drawing draws them: an out-of-order
 * room says *who* declared it unusable, and a stop-sell says *why* it was held
 * back. Both are the difference between a number and an answer — the Suite row
 * showing `0 free` with four physically fine rooms is only defensible if the
 * screen can say a manager held them for a wedding party.
 */
export interface TypeAvailability {
  roomType: string;

  /** `₹ 8 400 · gross` — value, currency and whether tax is included. */
  rate: string | null;

  total: number;
  sold: number;

  outOfOrder: number;

  /** Who says so, e.g. `EngineeringOps`. Null when the count is zero. */
  outOfOrderBy: string | null;

  stopSold: number;

  /** Why they are held, e.g. `wedding party`. Null when the count is zero. */
  stopSoldWhy: string | null;

  free: number;
}

/** The dates and party a search was made for — frame 14's three fields. */
export interface AvailabilityQuery {
  /** `3 Sep` — what the field shows. */
  arrive: string;

  /** `7 Sep`. */
  depart: string;

  /**
   * The same two dates in ISO, which is what travels.
   *
   * Two representations of one date, deliberately: `3 Sep` is what a
   * receptionist reads and is ambiguous about the year, and `2026-09-03` is
   * what a server can parse. Deriving one from the other on either side would
   * put date parsing in a screen — and a screen that guessed the year would
   * quote availability for a date twelve months away.
   */
  arriveOn: string;
  departOn: string;

  /** `1 room · 2 adults`. */
  party: string;
}

/** The answer, and what was asked — frame 14. */
export interface Availability {
  query: AvailabilityQuery;

  /** Null in a PMS-connected property, where the mode sentence differs. */
  mode: string;

  types: readonly TypeAvailability[];
}

/**
 * A room the desk is about to assign, and what already holds it — frame 14.
 *
 * **It warns and never forbids.** When staff answer *"two different stays"* to
 * a candidate link the room is genuinely double-booked, and that is the truth;
 * a hard block would make a ruled outcome unreachable. So the conflict names
 * the other stay and lets a person decide.
 */
export interface RoomConflict {
  room: string;
  headline: string;
  detail: string;
}

/**
 * The walk-in sheet's fields — frame 10.
 *
 * One action, because booking and arrival are one moment (S13): a two-step
 * "create, then check in" produces a stay in `Booked` that nobody ever leaves.
 * The **walk-in flag is set when the stay is created or it is unrecoverable**,
 * which is why it is a property of the draft rather than something recorded
 * afterwards.
 */
export interface WalkInDraft {
  guest: string;

  /** `new guest`, or the note that this person is already known. */
  guestNote: string;

  /**
   * What the desk typed. **A stay with none is valid and says so** — it is
   * never filled with a placeholder.
   */
  contact: string | null;
  contactKind: string;

  roomType: string;

  /** Check-in requires a room — the one hard gate the assignment ruling makes. */
  room: string | null;

  /** `vacant · clean`, read from Room Care where it is installed. */
  roomState: string | null;

  arrives: string;
  departs: string;

  /** `₹ 6 200.00 INR` — value and currency, with `gross` said separately. */
  rate: string | null;
  rateBasis: string;

  /** What marking this stay occupied will do, said before the button. */
  consequence: string;
}
