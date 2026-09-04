/**
 * The note a screen shows when it is drawing the approved example rather than
 * the property's own data — a manager must be able to tell which they are
 * looking at, so the difference is never hidden (ADR 0041's rule on what may
 * be said, applied by the seam).
 */

import { el } from "./element";

export function standIn(what: string, because: string | null): HTMLElement {
  const note = el("div", "note");
  note.append(
    el("b", undefined, `Showing the approved example ${what}. `),
    el("span", undefined, because ?? "The desktop has no Jobs client yet, so this is a stand-in."),
  );
  return note;
}
