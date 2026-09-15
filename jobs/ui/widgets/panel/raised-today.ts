/**
 * "Raised Today" — how much came in, how much went out, and of what kind. Z's
 * canvas frame `JobsRaised`, redrawn 2026-09-05 and owner-approved 2026-09-08.
 *
 * **Of what kind means by category.** The frame counted by *intent* — Fix ·
 * Prepare · Deliver · Check, citing "four intents" — and the walkthrough had
 * already removed the job `type` and put the catalogue's category › item in its
 * place. There was nothing to count by, which is why this one was redrawn
 * rather than amended.
 *
 * The tail line says how many categories did not fit, not how many jobs are in
 * them: "everything else · 4 categories" is a count of categories, and a reader
 * who took it for a job count would think the day was busier than it was.
 */

import { load, type HostApi } from "@hotelos/sdk";

import { el } from "../../chrome/element";
import { failure } from "../../chrome/failure";
import { JOB_READ } from "../../chrome/permissions";
import { type RaisedNow } from "../../board";
import { card, figures, openRow } from "../card";

export async function raisedToday(host: HostApi): Promise<HTMLElement> {
  const got = await load<RaisedNow>(host, JOB_READ, "widgetRaised");

  // **A widget shows its property's figures or says why it cannot.**
  // There is no recorded argument to fall back to any more, which is the
  // point: the seam makes the old behaviour unwriteable rather than
  // forbidden (owner, 2026-09-09).
  if (!got.ok) return card("Raised Today", "this property", [failure(got.failure, "today's jobs")]);

  const now = got.value;

  const body: (Node | null)[] = [
    figures([
      { value: String(now.raised), label: "raised today", tone: "run" },
      { value: String(now.closed), label: "closed today", tone: "ok" },
    ]),
  ];

  if (now.byCategory.length > 0) body.push(el("div", "wquiet", "By category"));
  for (const line of now.byCategory) {
    body.push(openRow(
      host,
      line.name,
      `${line.department} · ${line.count}`,
      "run",
      `jobs:board?raised=today&category=${encodeURIComponent(line.name)}`,
    ));
  }

  if (now.byCategory.length === 0) body.push(el("div", "wquiet", "Nothing has been raised today."));

  if (now.otherCategories > 0) {
    body.push(el("div", "wrefusal", `Everything else · ${now.otherCategories} categories`));
  }

  return card("Raised Today", "this property", body);
}
