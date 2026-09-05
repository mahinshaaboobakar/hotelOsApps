/**
 * Scheduled — frame 6: jobs set for a day, waiting for it. No cycle column:
 * the recurrence is the Engineering app's plan; Jobs sees one job per
 * occurrence and shows the raiser (owner, 2026-09-04).
 */

import type { HostApi } from "@hotelos/sdk";

import { el, fill } from "../../chrome/element";
import { day, when } from "../../chrome/instant";
import { tag } from "../../chrome/marks";
import { JOB_READ } from "../../chrome/permissions";
import { standIn } from "../../chrome/standin";
import { pager } from "../../chrome/tabs";
import { load } from "../../board";
import { recordedScheduled } from "../../board/recorded/live";

export async function scheduled(host: HostApi, main: HTMLElement): Promise<void> {
  const got = await load(host, JOB_READ, "scheduled", recordedScheduled);
  const t = el("table");
  const head = el("tr");
  for (const h of ["Scheduled for", "Job", "Where", "What", "Raised by", "Assigned to", "Due"]) head.append(el("th", undefined, h));
  t.append(head);
  for (const r of got.value) {
    const what = el("td", undefined, r.what);
    for (const x of r.tags) what.append(tag(x));
    const tr = el("tr");
    tr.append(
      el("td", undefined, day(host, r.scheduledFor)), el("td", "num", r.number), el("td", undefined, r.where), what,
      el("td", undefined, r.raisedBy), el("td", undefined, r.assignedTo), el("td", undefined, when(host, r.dueAt)),
    );
    t.append(tr);
  }

  const body = fill(
    el("div", "body"),
    t,
    el("div", "mono", "A scheduled job becomes RAISED at 00:00 on its day and its concern clock starts then. What put it here — a person, or the Engineering app's PPM plan — is only the raiser; Jobs holds the job, not the plan."),

    // The pager is the list's floor, so nothing follows it — the note that
    // explains the list belongs with the list, above (standard §6).
    // The real pager, on one page: the count is what tells a person the list in
    // front of them is the whole list, and the arrows are drawn disabled rather
    // than omitted — a pager that changes shape between one page and two is two
    // controls (standard §6).
    pager(`1–${String(got.value.length)} of ${String(got.value.length)}`, 0, 1, () => {}),
  );
  if (!got.live) body.append(standIn("scheduled list", got.because));
  main.replaceChildren(body);
}
