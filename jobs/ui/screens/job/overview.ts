/**
 * The Overview tab — frame 2: what and where, who asked, priority and time,
 * assignment, resolution. What you need to act.
 */

import type { HostApi } from "@hotelos/sdk";

import { el, fill } from "../../chrome/element";
import { words } from "../../chrome/wire";
import type { Detail, JobDetail } from "../../board";

export function overview(host: HostApi, d: JobDetail): HTMLElement {
  // The backend sends each value as data — an ISO instant, a day, a token — and
  // the cards say it in the property's words (tests/wire-shaped.test.ts).
  const said = (lines: readonly Detail[]): Detail[] => lines.map((l) => ({ ...l, v: words(host, l.v) }));
  const left = fill(el("div", "stack"), card("What and where", said(d.whatAndWhere)), card("Who asked", said(d.whoAsked)));
  const right = fill(
    el("div", "stack"),
    card("Priority and time", said(d.priorityAndTime)),
    card("Assignment", said(d.assignment)),
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
