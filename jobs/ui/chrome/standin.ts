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
    // **The default sentence outlived its fact.** It read "the desktop has no
    // Jobs client yet" — true on 2026-09-04, false from the day the module
    // envelope carried a call. What is actually true when the service says
    // nothing a person can act on is that the answer did not arrive, so that is
    // what it says now.
    el("span", undefined, because ?? "The service did not answer, so this is the approved example rather than this property's own."),
  );
  return note;
}
