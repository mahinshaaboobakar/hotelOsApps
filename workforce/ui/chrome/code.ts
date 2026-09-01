/**
 * A short code, as the design draws it everywhere.
 *
 * # Third consumer, so it stops being copied
 *
 * The legend and the picker each built this inline, and the audit found four
 * more places drawing a short code as plain text where the frames draw a chip —
 * the rota's posted column, the People zone, the catalogue table and the printed
 * legend. A code is the one thing that survives losing colour, and the chip is
 * what makes it findable at a glance; four screens disagreeing about how to draw
 * it is four screens that read as four products.
 */

import { el } from "./element";

/**
 * Build a code chip.
 *
 * @param code the short code, as the property typed it
 * @param tone the catalogue's tone, or "neutral"
 * @returns the chip
 */
export function codeChip(code: string, tone = "neutral"): HTMLElement {
  return el("b", `code ${tone}`, code);
}

/**
 * A colour, named with the dot that shows it.
 *
 * The frames pair the word with a swatch, because "Cyan" tells somebody what was
 * chosen and the dot tells them what it looks like — and the second is the one
 * that matters when reading a rota.
 */
export function colourDot(name: string, tone: string): HTMLElement {
  const row = el("span", "swatch-row");
  row.append(el("i", `dot ${tone}`), el("span", undefined, name));
  return row;
}
