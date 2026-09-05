/**
 * This application's own settings. Frame 16.
 */

import type { Tag } from "./day";

/** One label–value line of a settings card. */
export interface SettingRow {
  label: string;
  value: string;

  /** The part the design sets bold — a country, a deadline. */
  strong?: string;

  /** Trailing text after the bold part. */
  tail?: string;

  /** Italic-muted, for a reason somebody typed. */
  quiet?: string;

  /** A quieter trailing note — who set it, and when. */
  note?: string;

  tags: readonly Tag[];
}

/**
 * What sits under a card's rows, in the order the design puts it.
 *
 * **The order is data, not the screen's**, for the same reason the registration
 * card's rows are: frame 16's three cards each order these differently —
 * Stop-sell runs hint then button, Due to file runs buttons then hint, and
 * Guest reporting has only a note. A renderer with a fixed order gets two of
 * the three wrong, and the one it gets right is an accident.
 */
export type SettingBlock =
  | { readonly kind: "note"; readonly text: string }
  | { readonly kind: "hint"; readonly text: string }
  | { readonly kind: "actions"; readonly labels: readonly string[] };

/** A card of settings, with whatever the design hangs in its header. */
export interface SettingCard {
  title: string;

  /** The right of the header — a state pill, a count, an aside. */
  aside: Tag | string | null;

  rows: readonly SettingRow[];

  /** Everything under the rows, in the design's order. */
  blocks: readonly SettingBlock[];
}

/**
 * The settings screen — frame 16.
 *
 * **A property capability, not a country's law compiled into the product.** A
 * property with no reporting obligation turns it off and never sees the tab,
 * the flag or the list. Two required field sets, and the property decides both,
 * which is what lets one product serve a hotel in Kochi and a hotel in Dubai
 * **with no country written into it**.
 *
 * **The deadline is an offset, never a date** — *24 hours after arrival*, for
 * the same reason a cancellation deadline is (R18): move the arrival and the
 * deadline moves with it.
 *
 * **And the one honest limit is on the screen itself**: HotelOS does not submit
 * anything. Sending a filing automatically is an integration, every integration
 * on this platform is a connector, and that connector does not exist. It is
 * reported rather than drawn as a button that would not work.
 */
export interface Setup {
  /** The section tabs — Registration, Guest reporting, Stop-sell, Stay defaults. */
  sections: readonly { label: string; on: boolean }[];

  /** The full-width card above the two columns. */
  lead: SettingCard;

  /** The two side by side. */
  pair: readonly [SettingCard, SettingCard];

  /**
   * The card at the foot, whose body is two columns of rows rather than one
   * list — the required sets read as a comparison and a single column would
   * make them read as a sequence.
   */
  card: {
    title: string;
    left: readonly SettingRow[];
    right: readonly SettingRow[];
    hint: string;
  };
}
