/**
 * The shapes the three screens read — the approved design's vocabulary, typed.
 *
 * These are **view shapes, not the domain**. The service's `RoomStay` carries a
 * lifecycle enum and a version; a table row carries the string a receptionist
 * reads. Keeping them apart is what lets the screens be built to the gold
 * frames while the wire contract is still `service.proto`'s.
 *
 * Every field here exists because a gold frame draws it. Nothing is here
 * speculatively — a field the design does not show is a field the module has no
 * way to render honestly.
 */

/**
 * The five marks that carry the whole application, plus the two states the
 * frames give their own colour.
 *
 * `pms` · `override` · `disagrees` · `missing` are the four the design names.
 * `other` marks what GuestOps does not own and reads through Context.
 * `dayuse` and `unknown` are frame 1's and frame 12's own tones.
 */
export type Mark = "pms" | "override" | "disagrees" | "other" | "missing" | "dayuse" | "unknown";

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
  tone: Mark | "ok" | "warn" | "neutral";
  text: string;
}

/** One row of the day's table — gold frame 1. */
export interface DayRow {
  id: string;
  guest: string;

  /**
   * Masked, always — GUEST-Q7. The desk sees `+91 98470 •••• 12` and reveals it
   * with the permission that lets them act on the stay.
   */
  contact: string;

  /** True for a party member with no name yet, drawn italic in the design. */
  unnamed: boolean;

  /** `BK-4471 · 1 of 3` — the group position is a designed signal. */
  booking: string;

  roomType: string;

  /** Null renders the design's inline `＋ assign` action, not a static chip. */
  room: string | null;

  /** `31 Aug → 2 Sep`, or the day-use form `31 Aug · day use`. */
  nights: string;

  chips: readonly Chip[];
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

/** The front desk day — gold frame 1. */
export interface Today {
  businessDate: string;
  rollsAt: string;

  /** Decides the mode sentence: Opera writes the lifecycle, or this is the book. */
  connected: boolean;

  stats: readonly Stat[];
  lists: readonly DayList[];
}

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

/** The amber band: a disagreement standing over an override. */
export interface Banner {
  headline: string;
  detail: string;
  attribution: string;

  /** The first is the primary. Both values stay on the record either way. */
  actions: readonly string[];
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
}

/** The stay page — gold frame 3. */
export interface StayPage {
  id: string;
  guest: string;
  room: string | null;
  stayId: string;
  bookingRef: string;

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

/** One label–value row inside an attention card. */
export interface AttentionRow {
  label: string;
  value: string;
  strong?: string;
  tail?: string;
  tags: readonly Tag[];
}

/** One thing a person has to decide — gold frame 12. */
export interface AttentionCard {
  id: string;

  /** The band naming the class of problem: `Same stay, or two?`. */
  kind: string;

  /** The right-hand side of the band — a chip, or plain text. */
  status: Chip | string | null;

  rows: readonly AttentionRow[];

  /** The dashed box. Null when the card explains itself in a hint instead. */
  note: string | null;

  /** A quieter line, where the design uses one rather than the dashed box. */
  hint: string | null;

  /** The first is the primary. Empty when the card is informational. */
  actions: readonly string[];
}
