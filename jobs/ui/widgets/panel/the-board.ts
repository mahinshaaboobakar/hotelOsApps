/**
 * "The Board" — the shape of the work right now, and what has been waiting
 * longest with nobody on it. Z's canvas frame `JobsBoard`, owner-approved
 * 2026-09-03; built 2026-09-05 after this stream's review found it computable
 * exactly as drawn.
 */

import { load, type HostApi } from "@hotelos/sdk";

import { el } from "../../chrome/element";
import { failure } from "../../chrome/failure";
import { JOB_READ } from "../../chrome/permissions";
import { type BoardNow } from "../../board";
import { card, figures, openRow } from "../card";

export async function theBoard(host: HostApi): Promise<HTMLElement> {
  const got = await load<BoardNow>(host, JOB_READ, "widgetBoard");

  // **A widget shows its property's figures or says why it cannot.**
  // There is no recorded argument to fall back to any more, which is the
  // point: the seam makes the old behaviour unwriteable rather than
  // forbidden (owner, 2026-09-09).
  if (!got.ok) return card("The Board", "this property", [failure(got.failure, "the shape of the work")]);

  const now = got.value;

  const body: (Node | null)[] = [
    figures([
      { value: String(now.raised), label: "new", tone: "" },
      { value: String(now.running), label: "in progress", tone: "run" },
      { value: String(now.onHold), label: "on hold", tone: "warn" },
      { value: String(now.doneToday), label: "done", tone: "ok" },
    ]),
    el("div", "wquiet", "Longest in NEW — nobody has taken these"),
  ];

  for (const row of now.longestWaiting) {
    // The destination carries the filter, so the screen opens on the same
    // question the widget answered — a tap-through that landed on an unfiltered
    // board would make the person find the row again.
    body.push(openRow(host, row.number, row.since, row.tone, `jobs:board?status=RAISED&job=${row.number}`));
  }

  if (now.longestWaiting.length === 0) body.push(el("div", "wquiet", "Nothing is waiting unclaimed."));

  // The frame's own footnote, kept: it says what these four figures are not.
  body.push(el("div", "wrefusal", "ASSIGNED, ACCEPTED, PAUSED and CANCELLED are counted in the app, not here."));

  return card("The Board", "this property", body);
}
