/**
 * Deep clean — due, planned, blocked, in the job's hands, returning (frame 6;
 * S0). Room Care plans and requests; the state's owner places the block and
 * Jobs does the work. Two ports drawn honestly: the block reads "requested"
 * until its owner's event arrives, and the job's progress is open / closed
 * until JOBS-Q2 publishes more.
 */

import type { HostApi } from "@hotelos/sdk";

import { card } from "../../chrome/card";
import { failed } from "../../chrome/failure";
import { pager, scroller } from "../../chrome/bar";
import { control, el } from "../../chrome/element";
import { day, when } from "../../chrome/instant";
import { READ, act, holds, load } from "../../chrome/load";
import type { Nav } from "../../chrome/nav";
import { whole } from "../../chrome/number";
import { actions, dialog, readyWhen, sheet } from "../../chrome/overlay";
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
  const got = await load<Page>(host, READ, "deepCleans", { page });
  if (!got.ok) {
    body.append(failed(host, got.failure, "the deep cleans", nav.show));
    return;
  }

  const v = got.value;
  const strip = el("div", "strip");
  const count = (n: number, label: string): HTMLElement => { const c = el("span"); c.append(el("b", undefined, whole(host, n)), document.createTextNode(label)); return c; };
  strip.append(count(v.dueThisMonth, "due this month"), count(v.planned, "planned"), count(v.inProgress, "in progress"),
    el("span", undefined, v.plan.length === 0 ? "no plan set — Setup › Deep clean plan" : `plan: ${v.plan.map((p) => `${p.roomType} every ${whole(host, p.everyMonths)} months`).join(" · ")}`));

  const table = el("table", "list");
  const head = el("tr");
  for (const name of ["Room", "Type", "Last done", "Due", "Window", "Block", "Job", "State"]) head.append(el("th", undefined, name));
  table.append(head);
  for (const row of v.rows) {
    const tr = el("tr");
    const block = el("td");
    if (row.blockAppliedAt !== null) block.append(el("span", "pill ok", `applied ${day(host, row.blockAppliedAt.slice(0, 10))}`));
    else if (row.blockRequestedAt !== null) block.append(el("span", "pill soft-warn", `requested ${when(host, row.blockRequestedAt)}`));
    else block.append(document.createTextNode("—"));
    const job = el("td", "mono");
    job.append(document.createTextNode(row.jobId === null ? (row.blockRequestedAt === null ? "—" : "raised with the plan") : row.jobStatus === null ? "job raised — no status heard yet " : `job ${lower(row.jobStatus)} `));
    const state = el("td");
    const tone = row.state === "DUE" ? "warn" : row.state === "IN_PROGRESS" ? "run" : row.state === "DONE" ? "ok" : "";
    state.append(el("span", `pill ${tone}`, row.state.replaceAll("_", " ")));
    if (row.state === "DUE" && holds(host, "roomcare.plan")) state.append(document.createTextNode(" "), control("btn sm", "plan a window…", () => plan(host, nav, row)));
    tr.append(el("td", "num", row.room), el("td", undefined, row.type), el("td", undefined, row.lastDone === null ? "never recorded" : day(host, row.lastDone)),
      el("td", undefined, day(host, row.due)), el("td", undefined, row.windowFrom === null ? "—" : `${day(host, row.windowFrom)} – ${day(host, row.windowTo)}`), block, job, state);
    table.append(tr);
  }

  body.append(strip, scroller(table), pager(host, v.paging, v.rows.length, "deep cleans due or under way", goPage));
  const underWay = v.rows.find((r) => r.jobId !== null);
  if (underWay !== undefined) body.append(progress(host, nav, underWay));
}

/** The job's progress card — open or closed only, until JOBS-Q2 publishes more (the port, drawn honestly). */
function progress(host: HostApi, nav: Nav, row: Row): HTMLElement {
  const view = card(`${row.room} — the job's progress`);
  view.style.cssText = "margin-top:14px;flex:none";
  const kv = el("div", "kv");
  kv.append(el("div", "k", "Job"), el("div", undefined, `${row.jobStatus === null ? "job raised — no status heard yet" : `job ${lower(row.jobStatus)}`} · window ${day(host, row.windowFrom)} – ${day(host, row.windowTo)}`),
    // "Hands" (who works the job, by day) is not drawn: Room Care hears only that a job opened and closed until
    // JOBS-Q2, and the reason was a register id and two event names on the screen (owner ruling, 2026-09-19).
    el("div", "k", "On close"), el("div", undefined, `${row.room} → dirty → departure clean → clean again → release requested → sold again`));
  view.append(kv);
  if (holds(host, "roomcare.plan")) {
    const line = el("div", "row");
    line.style.marginTop = "10px";
    line.append(control("btn danger", "Cancel this deep clean…", () => void cancel(host, nav, row)));
    view.append(line);
  }
  return view;
}

function plan(host: HostApi, nav: Nav, row: Row): void {
  const overlay = sheet(nav.frame, `Room ${row.room} — plan a deep clean`);
  const from = el("input", "field") as HTMLInputElement;
  from.type = "date";
  const to = el("input", "field") as HTMLInputElement;
  to.type = "date";
  overlay.body.append(
    el("label", "lbl", "From"), from, el("label", "lbl", "To"), to,
    el("p", "dim", "The room leaves the day while it is blocked."),
  );
  actions(overlay, "Plan and request", () => void (async () => {
    const done = await act(host, "roomcare.plan", "planDeepClean", { roomId: row.roomId, from: from.value, to: to.value });
    if (!done.ok) return overlay.refuse(done.because);
    overlay.close();
    nav.show();
  })());
  readyWhen(overlay, () => (from.value === "" || to.value === "" ? "choose both dates" : null));
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
