/**
 * The Work tab — frame 2b: the clock now, and the sessions table. PAUSED lives
 * here and is never a job status (S2 D2).
 */

import type { HostApi } from "@hotelos/sdk";

import { control, el, fill } from "../../chrome/element";
import { elapsed, when } from "../../chrome/instant";
import type { JobDetail, Session } from "../../board";

export function work(host: HostApi, d: JobDetail, mayResolve: boolean, onResolve: () => void): HTMLElement {
  const grid = el("div", "cols");
  grid.style.gridTemplateColumns = "1fr 2fr";
  grid.append(now(d, mayResolve, onResolve), sessions(host, d.sessions));
  return grid;
}

function now(d: JobDetail, mayResolve: boolean, onResolve: () => void): HTMLElement {
  const box = el("div", "card");
  box.append(el("h3", undefined, "Now"));
  const kv = el("div", "kv");
  kv.style.gridTemplateColumns = "100px 1fr";
  kv.append(
    el("div", "k", "Working"), el("div", undefined, d.runningWho === null ? "nobody" : d.runningWho),
    el("div", "k", "This session"), el("div", undefined, d.runningSeconds === null ? "—" : elapsed(d.runningSeconds)),
    el("div", "k", "All sessions"), el("div", undefined, elapsed(d.totalWorkedSeconds)),
    el("div", "k", "Promise"), el("div", undefined, d.priorityAndTime.find((x) => x.k === "Due at")?.v ?? "—"),
  );
  box.append(kv);
  const working = d.runningSeconds !== null && d.row.viewerIsAssignee;
  if (working || mayResolve) {
    const row = el("div", "row");
    if (working) row.append(control("btn", "Pause"), control("btn", "Stop"));
    if (mayResolve) row.append(control("btn pri", "Resolve…", onResolve));
    box.append(row);
  }
  return box;
}

function sessions(host: HostApi, list: readonly Session[]): HTMLElement {
  const box = el("div", "card");
  box.append(el("h3", undefined, "Sessions"));
  const t = el("table");
  const head = el("tr");
  for (const h of ["#", "Who", "Started", "Paused · why", "Resumed", "Stopped", "Worked"]) head.append(el("th", undefined, h));
  t.append(head);
  for (const s of list) {
    const tr = el("tr");
    tr.append(
      el("td", undefined, String(s.no)), el("td", undefined, s.who), el("td", undefined, when(host, s.startedAt)),
      el("td", undefined, s.pausedAt === null ? "—" : `${when(host, s.pausedAt)} · ${s.pauseReason ?? ""}`),
      el("td", undefined, s.resumedAt === null ? "—" : when(host, s.resumedAt)),
      fill(el("td"), s.stoppedAt === null ? el("span", "pill run", "running") : document.createTextNode(when(host, s.stoppedAt))),
      el("td", undefined, elapsed(s.workedSeconds)),
    );
    t.append(tr);
  }
  box.append(t, el("div", "mono", "A pause keeps the session; a stop ends it. Resolve stops the running one."));
  return box;
}
