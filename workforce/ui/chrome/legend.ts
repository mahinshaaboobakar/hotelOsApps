/**
 * The catalogue legend — what the short codes mean.
 *
 * # It is not decoration
 *
 * A rota is read by its short codes, and a code is only legible to somebody who
 * knows the catalogue. The Staff Schedule and the Team Rota both carry this
 * strip, and the printed week carries the same list doing more work — which is
 * why it lives in `chrome/` rather than in either screen: two copies would
 * drift, and a legend that disagreed with another legend is worse than none.
 */

import { el } from "./element";
import type { Shift } from "../roster";

/**
 * Build the strip.
 *
 * @param catalogue the property's shifts
 * @param note a sentence to close with, when the screen has one
 * @returns the legend
 */
export function legend(catalogue: readonly Shift[], note?: string): HTMLElement {
  const strip = el("div", "legend");

  strip.append(el("div", "llab", "This property's shifts"));

  for (const shift of catalogue) {
    const entry = el("div", "lent");

    entry.append(
      el("b", `code ${shift.tone}`, shift.code),
      el("span", undefined, shift.name),
      el("s", undefined, shift.hours ?? ""),
    );

    strip.append(entry);
  }

  if (note !== undefined) {
    strip.append(el("div", "lnote", note));
  }

  return strip;
}
