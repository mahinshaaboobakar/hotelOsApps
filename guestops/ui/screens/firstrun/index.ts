/**
 * Install day, on a property that already has a book. Gold frame 13.
 *
 * **The empty state that would be wrong here is the usual one** — *no
 * reservations yet, create your first booking* — on a property with two
 * thousand of them waiting. The Integration Hub has been normalising
 * reservation and guest facts since the connector shipped and holding them
 * deferred, with their business date and provenance, precisely because this
 * domain did not exist to own them (ADR 0128). Installing this application is
 * what turns that queue on.
 *
 * **The counts are the Hub's, not an estimate.** They are what is actually
 * held, which is why the screen can say them: a progress screen quoting a
 * number nobody counted is the same defect as a balance nobody computed.
 */

import type { FirstRun } from "../../book";
import { control, el } from "../../chrome/element";

/**
 * Draw the screen.
 *
 * @param run what is being replayed, and since when
 * @param progress what the Show progress action does
 * @returns the screen
 */
export function firstRun(run: FirstRun, progress?: () => void): HTMLElement {
  const empty = el("div", "empty");
  const what = el("p");
  what.append(el("b", undefined, run.what), document.createTextNode(` ${run.since}`));

  empty.append(
    el("div", "ic", "⌛"),
    el("b", undefined, run.headline),
    what,
    el("p", "quiet", run.reassurance),
    control("btn sm pri", "Show progress", progress),
  );

  return empty;
}
