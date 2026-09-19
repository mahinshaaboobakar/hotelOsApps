/**
 * An overlay — a sheet where a person composes, a dialog where a person
 * confirms. Page 64 §9.
 *
 * # One vocabulary, and the scrim decides which
 *
 * *"Two placements, one vocabulary. A sheet enters from the right and is where
 * a person composes something. A dialog sits in the middle and is where a person
 * confirms something. Same head, body and foot; the scrim decides which."*
 *
 * Every overlay in this module used to assemble its own `scrim` and `dlg` by
 * hand, and the app surface audit (2026-09-19, O1 · O7) found what nine copies
 * of one shape become: the composing forms drawn as centred boxes, a confirm at
 * 440 rather than 520, the one sheet at 390, and none on §9's shared
 * `.dh .db .df`. A screen now says only WHICH it is; the shape is here once.
 *
 * # Three rules this keeps, from §9
 *
 * * **`position:absolute`, never `fixed`** — a module runs in an iframe (the
 *   stylesheet's `.scrim`).
 * * **The overlay is a sibling of `.body`, not a child** — the caller appends it
 *   to the screen's mount, never into the body.
 * * **The scrim dismisses; the surface does not** — a click inside a sheet is a
 *   person working in it.
 */

import { el } from "./element";

/** What the person is doing when they arrive — §9's own test. */
export type OverlayKind = "sheet" | "dialog";

/** The three parts every overlay has. */
export interface OverlayParts {
  /** The heading and its sub-line. */
  head: readonly Node[];

  /** What the person reads or fills in. */
  body: readonly Node[];

  /** The actions — usually one `.acts` row. */
  foot: readonly Node[];
}

/**
 * Build an overlay.
 *
 * @param kind `sheet` to compose, `dialog` to confirm
 * @param parts the head, body and foot
 * @param close called when the scrim is clicked
 * @returns the scrim, holding the surface — append it beside `.body`
 */
export function overlay(kind: OverlayKind, parts: OverlayParts, close: () => void): HTMLElement {
  const scrim = el("div", kind === "dialog" ? "scrim mid" : "scrim");
  const surface = el("div", kind === "dialog" ? "dlg" : "sheet");

  const head = el("div", "dh");
  head.append(...parts.head);
  const body = el("div", "db");
  body.append(...parts.body);
  const foot = el("div", "df");
  foot.append(...parts.foot);

  surface.append(head, body, foot);
  scrim.append(surface);

  scrim.addEventListener("click", (event) => {
    if (event.target === scrim) close();
  });

  return scrim;
}
