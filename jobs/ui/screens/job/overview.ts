/**
 * The Overview tab — frame 2: what and where, who asked, priority and time,
 * assignment, resolution. What you need to act.
 */

import { el, fill } from "../../chrome/element";
import type { Detail, JobDetail } from "../../board";

export function overview(d: JobDetail): HTMLElement {
  const left = fill(el("div", "stack"), card("What and where", d.whatAndWhere), card("Who asked", d.whoAsked));
  const right = fill(
    el("div", "stack"),
    card("Priority and time", d.priorityAndTime),
    card("Assignment", d.assignment),
    resolution(d.resolution),
  );
  return fill(el("div", "cols"), left, right);
}

/** A card of key/value lines — the Overview's idiom, reused by Record. */
export function card(title: string, lines: readonly Detail[]): HTMLElement {
  const box = el("div", "card");
  box.append(el("h3", undefined, title));
  const grid = el("div", "kv");
  for (const line of lines) grid.append(el("div", "k", line.k), el("div", undefined, line.v));
  box.append(grid);
  return box;
}

function resolution(text: string | null): HTMLElement {
  const box = el("div", "card");
  box.append(el("h3", undefined, "Resolution"), el("div", "mono", text ?? "— open. Filled by the Resolve step."));
  return box;
}
