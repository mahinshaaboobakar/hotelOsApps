import { failureDrawing, type ReadFailure, FAILURE_LABELS } from "@hotelos/sdk";

import { control, el, fill } from "./element";

/**
 * What a screen draws when a read did not arrive — the SDK's words, this
 * module's geometry.
 *
 * **No screen renders recorded rows. Ever** (owner, 2026-09-09: *"showing a
 * hardcoded list is wrong"*). This replaces `standIn()`, which drew the
 * approved example under a banner saying so: honest about the page, and still
 * a list of jobs that do not exist, in a hotel where somebody may act on it.
 *
 * The sentence, the mark and whether a retry is offered are
 * {@link failureDrawing}'s, so three applications say one thing; the elements
 * are this chrome's, because a module's surface is its own. The split is
 * deliberate — a shared component would cross the realm, and shared *words*
 * do not.
 */
export function failure(
  said: ReadFailure,
  the: string,
  again?: () => void,
): HTMLElement {
  const drawing = failureDrawing(said, { app: "Jobs", the });
  const panel = el("div", "gap");

  panel.append(
    el("div", "gap-mark", drawing.mark),
    el("div", "gap-said", drawing.said),
    el("div", "gap-why", drawing.why),
  );

  // Only a read that never arrived can be retried — a refusal needs a grant and
  // a fault needs a fix, and a button that promises either is a lie the
  // platform cannot keep. The seam decides; this only honours it.
  if (drawing.retryable && again !== undefined) {
    panel.append(fill(el("div", "row"), control("btn", FAILURE_LABELS.retry, again)));
  }

  panel.append(el("div", "gap-wire", drawing.wire));
  return panel;
}
