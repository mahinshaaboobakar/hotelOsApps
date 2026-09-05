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
