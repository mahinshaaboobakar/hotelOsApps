/**
 * The widget realm's stylesheet — the card's rules and the failed card's.
 *
 * **Its own file so neither surface imports the other.** The failed card is
 * built from the card, and the sheet needs both; composing it in `card.ts`
 * would make the two files import each other.
 *
 * **The failed card's rules are not optional here, and their absence is what
 * made the 0.4.0 round.** A widget is its own realm and shares no sheet with
 * the module, and today every read it makes is refused — so the state this realm
 * is always in was once the one state it had no rules for. `tests/styled.test.ts`
 * calls this builder and fails if they stop being composed.
 */

import { WIDGET_CSS } from "./card";
import { FAILED_CSS } from "./failed";

/** Added once per draw. */
export function stylesheet(): HTMLStyleElement {
  const style = document.createElement("style");
  style.textContent = [WIDGET_CSS, FAILED_CSS].join("\n");
  return style;
}
