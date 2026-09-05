/**
 * The day — its marks, its rows, and the tabs that count them. Frame 1.
 */

/**
 * The five marks that carry the whole application, plus the two states the
 * frames give their own colour.
 *
 * `pms` · `override` · `disagrees` · `missing` are the four the design names.
 * `other` marks what GuestOps does not own and reads through Context.
 * `dayuse` and `unknown` are frame 1's and frame 12's own tones, and `walkin`
 * and `note` are frame 2's.
 */
export type Mark =
  | "pms"
  | "override"
  | "disagrees"
  | "other"
  | "missing"
  | "dayuse"
  | "unknown"

  /** A stay this desk created rather than received — frame 2. */
  | "walkin"

  /**
   * A plain aside beside the marks — frame 2's `penalty applied`.
   *
   * It is in the mark list because it sits in the marks *column*, and it is
   * not a mark: the drawing gives it no dot, no tint and no border. Rendering
   * it as one would dress a footnote as provenance.
   */
  | "note";

/** A mark chip: a dot and a word. */
export interface Chip {
  mark: Mark;
  text: string;
}

/**
 * Something attached to a value.
 *
 * The design uses four visually distinct kinds and they mean different things,
 * so they are different kinds here rather than one styled string:
 * a **mark** says where a value came from, a **lock** says how it was
 * established (`OBSERVED`, `FROM OPERA`), a **pill** is a state (`complete`,
 * `same room`), and a **link** is an action inline in the value.
 *
 * `text` is the fifth and exists because the design **interleaves** plain words
 * between the others — the contact row reads
 * `+91 … [reveal] [MOBILE · PRIMARY] · rajesh.p@… [reveal]`. Without it the
 * caller would have to smuggle that text through a pill, which is how a plain
 * email ends up wearing a state chip's background.
 */
export interface Tag {
  kind: "mark" | "lock" | "pill" | "link" | "text";

  /**
   * `bad` is here because two things need it and they are not the same thing:
   * a **pill** for a state that is over or overdue, and a **lock** for a value
   * the platform cannot establish. Both are refusals of a kind, and giving them
   * one tone keeps a screen from having two reds that mean different things.
   */
  tone: Mark | "ok" | "warn" | "neutral" | "bad";

  text: string;
}

/** One row of the day's table — gold frame 1. */
export interface DayRow {
  id: string;
  guest: string;

  /**
   * Absent, and reported as absent — GUEST-Q12, ruled 2026-09-04.
   *
   * The design draws `+91 98470 •••• 12` and GUEST-Q7 rules it masked. Neither
   * is producible: contacts are stored encrypted and `IContactProtector` has
   * only a write direction, so nothing in this process can mask a value it
   * cannot read. **Null renders as nothing.** The fixture carries null too —
   * a recorded row showing a number the live row can never show would make
   * every capture of this screen overstate what is built.
   */
  contact: string | null;

  /**
   * `party of 2` — how many people the source said, where the row has no name.
   *
   * Its own field rather than a second meaning for `contact`, because it is a
   * *count* and counts are producible. Folding it into the contact slot would
   * put one field in front of two facts, and the day the contact becomes
   * readable this row would start showing a phone number where it used to say
   * how many people were coming.
   */
  party: string | null;

  /** True for a party member with no name yet, drawn italic in the design. */
  unnamed: boolean;

  /** `BK-4471 · 1 of 3` — the group position is a designed signal. */
  booking: string;

  roomType: string;

  /** Null renders the design's inline `＋ assign` action, not a static chip. */
  room: string | null;

  /** `31 Aug → 2 Sep`, or the day-use form `31 Aug · day use`. */
  nights: string;

  /**
   * What the row carries at its right.
   *
   * `Tag`, not `Chip`, because frame 11 mixes them: `override` is a **mark**
   * saying where a value came from, and `confirmed 15:44` is a **pill** saying
   * what state the row is in. They are drawn differently on purpose
   * (`chrome/marks.ts`), and a single kind here would force one to wear the
   * other's shape.
   */
  chips: readonly Tag[];
}

/** One tab of the day, with the count the tab carries. */
export interface DayList {
  key: string;
  label: string;
  count: string;
  rows: readonly DayRow[];
}

/** A stat-strip tile. The first is selected — it is a filter, not a label. */
export interface Stat {
  value: string;
  label: string;
}

/**
 * Something that has stopped arriving — gold frame 11.
 *
 * **Per capability, and it gates nothing.** A connector can be authenticated,
 * polling and green while check-ins specifically have stopped (R27), so the
 * banner names *what* is late rather than declaring the feed down. And it
 * changes no rule: the property is PMS-writes-first at all times, so an
 * override means one thing in every condition and the screen always says the
 * same true thing — **your action stands** (S36, GUEST-Q4).
 */
export interface Staleness {
  /** `PMS feed silent since 09:00 — your entries stand.` */
  headline: string;

  /** What is late, and what is still arriving. */
  detail: string;
}

/** The front desk day — gold frames 1 and 11. */
export interface Today {
  businessDate: string;
  rollsAt: string;

  /** Null when everything the property expects is arriving. */
  stale: Staleness | null;

  /** Decides the mode sentence: Opera writes the lifecycle, or this is the book. */
  connected: boolean;

  stats: readonly Stat[];
  lists: readonly DayList[];
}

/**
 * The book being filled for the first time — gold frame 13.
 *
 * **GuestOps's first day is not an empty book.** The Integration Hub has been
 * normalising reservation and guest facts since the connector shipped and
 * holding them *deferred*, with their business date and provenance, precisely
 * because this domain did not exist to own them (ADR 0128). Installing this
 * application is what turns that queue on.
 *
 * **The empty state that would be wrong here is the usual one** — *no
 * reservations yet, create your first booking* — on a property with two
 * thousand of them waiting.
 */
export interface FirstRun {
  /** `Bringing in what Opera already sent`. */
  headline: string;

  /** `2 314 reservations and 1 806 guests`, and since when. */
  what: string;
  since: string;

  /** What the desk may do while it runs. */
  reassurance: string;
}
