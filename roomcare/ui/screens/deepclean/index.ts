/**
 * Deep clean — due, planned, blocked, in the job's hands, returning (frame 6;
 * S0). Room Care plans and requests; the state's owner places the block and
 * Jobs does the work. Two ports drawn honestly: the block reads "requested"
 * until its owner's event arrives, and the job's progress is open / closed
 * until JOBS-Q2 publishes more.
 */

import type { HostApi } from "@hotelos/sdk";

import { pager } from "../../chrome/bar";
import { control, el } from "../../chrome/element";
import { day, when } from "../../chrome/instant";
import { act, failed, holds, load } from "../../chrome/load";
import type { Nav } from "../../chrome/nav";
import { actions, dialog, sheet } from "../../chrome/overlay";
import { lower } from "../../chrome/words";
import type { Paging } from "../../model";

interface Row {
  deepCleanId: string | null;
  version: number | null;
  roomId: string;
  room: string;
  type: string;
  lastDone: string | null;
  due: string;
  windowFrom: string | null;
  windowTo: string | null;
  blockRequestedAt: string | null;
  blockAppliedAt: string | null;
  jobId: string | null;
  jobStatus: string | null;
  state: string;
}

interface Page {
  dueThisMonth: number;
  planned: number;
  inProgress: number;
  plan: { roomType: string; everyMonths: number }[];
  rows: Row[];
  paging: Paging;
}

export async function deepClean(host: HostApi, body: HTMLElement, nav: Nav, page: number, goPage: (page: number) => void): Promise<void> {
  const got = await load<Page>(host, "deepCleans", { page });
  if (!got.ok) {
    body.append(failed("Deep cleans", got.because));
    return;
  }

  const v = got.value;
  const strip = el("div", "strip");
  const count = (n: number, label: string): HTMLElement => { const c = el("span"); c.append(el("b", undefined, String(n)), document.createTextNode(label)); return c; };
  strip.append(count(v.dueThisMonth, "due this month"), count(v.planned, "planned"), count(v.inProgress, "in progress"),
    el("span", undefined, v.plan.length === 0 ? "no plan set — Setup › Deep clean plan" : `plan: ${v.plan.map((p) => `${p.roomType} every ${p.everyMonths} months`).join(" · ")}`));

  const table = el("table", "list");
  const head = el("tr");
  for (const name of ["Room", "Type", "Last done", "Due", "Window", "Block", "Job", "State"]) head.append(el("th", undefined, name));
  table.append(head);
  for (const row of v.rows) {
    const tr = el("tr");
    const roomCell = el("td");
    roomCell.append(control("btn sm", row.room, () => nav.openRoom(row.roomId)));
    const block = el("td");
    if (row.blockAppliedAt !== null) block.append(document.createTextNode(`applied ${when(host, row.blockAppliedAt)}`));
    else if (row.blockRequestedAt !== null) block.append(document.createTextNode(`requested ${when(host, row.blockRequestedAt)} `), el("span", "tag port", "applied by the state's owner"));
    else block.append(document.createTextNode("—"));
    const job = el("td");
    job.append(document.createTextNode(row.jobId === null ? (row.blockRequestedAt === null ? "—" : "raised with the plan") : `job ${lower(row.jobStatus ?? "open")} `));
    if (row.jobId !== null) job.append(el("span", "tag port", "progress · JOBS-Q2"));
    const state = el("td");
    state.append(document.createTextNode(lower(row.state)));
    if (row.state === "DUE" && holds(host, "roomcare.plan")) state.append(document.createTextNode(" · "), control("btn sm", "plan a window…", () => plan(host, nav, row)));
    if (row.deepCleanId !== null && row.state !== "DONE" && holds(host, "roomcare.plan")) state.append(document.createTextNode(" · "), control("btn sm danger", "Cancel…", () => void cancel(host, nav, row)));
    tr.append(roomCell, el("td", undefined, row.type), el("td", "num", row.lastDone === null ? "never recorded" : day(host, row.lastDone)), el("td", "num", day(host, row.due)),
      el("td", "num", row.windowFrom === null ? "—" : `${day(host, row.windowFrom)} – ${day(host, row.windowTo)}`), block, job, state);
    table.append(tr);
  }

  body.append(strip, table, pager(v.paging, v.rows.length, "deep cleans due or under way", goPage));
}

function plan(host: HostApi, nav: Nav, row: Row): void {
  const overlay = sheet(nav.frame, `Room ${row.room} — plan a deep clean`);
  const from = el("input", "field") as HTMLInputElement;
  from.type = "date";
  const to = el("input", "field") as HTMLInputElement;
  to.type = "date";
  overlay.body.append(
    el("label", "lbl", "From"), from, el("label", "lbl", "To"), to,
    el("p", "dim", "Two requests leave with correlation ids: the block, to the owner of the room's out-of-order state, and the job, to Jobs. The room leaves the day while it is blocked."),
  );
  actions(overlay, "Plan and request", () => void (async () => {
    const done = await act(host, "roomcare.plan", "planDeepClean", { roomId: row.roomId, from: from.value, to: to.value });
    if (!done.ok) return overlay.refuse(done.because);
    overlay.close();
    nav.show();
  })());
}

async function cancel(host: HostApi, nav: Nav, row: Row): Promise<void> {
  const overlay = dialog(nav.frame, `Cancel room ${row.room}'s deep clean?`);
  overlay.body.append(el("p", undefined, "The block's release is requested from its owner. Recorded: who, when."));
  actions(overlay, "Cancel the deep clean", () => void (async () => {
    const done = await act(host, "roomcare.plan", "cancelDeepClean", { deepCleanId: row.deepCleanId, version: row.version });
    if (!done.ok) return overlay.refuse(done.because);
    overlay.close();
    nav.show();
  })(), "btn danger confirm");
}
