/**
 * The things a person has to decide. Frame 12.
 */

import type { Chip, Tag } from "./day";
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

/**
 * One page of the things a person has to decide.
 *
 * **The count is the list's, not the page's** — `64` §8. A screen that showed
 * nine cards and said "9 to decide" while eleven were waiting would be telling
 * a receptionist the morning is lighter than it is, which is the one thing this
 * screen exists not to do.
 */
export interface AttentionPage {
  total: number;
  cards: readonly AttentionCard[];
}
