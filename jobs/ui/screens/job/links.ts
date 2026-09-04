/**
 * The Links & steps tab — frame 2e: the job's steps in sequence, then its
 * group links. Two relations, drawn apart because they mean different things
 * (S1 D2).
 */

import { control, el, fill } from "../../chrome/element";
import { status } from "../../chrome/marks";
import type { JobDetail } from "../../board";

export function links(d: JobDetail, mayAmend: boolean): HTMLElement {
  const root = el("div");
  root.append(el("div", "sect", "Steps of this job · sequence"), steps(d), el("div", "mono", "Cancelling this job cancels its steps. Closing it never closes them (S1 D2)."));
  root.append(el("div", "sect", "Linked jobs · same room, related"), linked(d, mayAmend));
  if (mayAmend) {
    const row = el("div", "row");
    row.append(control("btn", "Link a job…"), control("btn", "Add a step…"));
    root.append(row);
  }
  return root;
}

function steps(d: JobDetail): HTMLElement {
  const t = el("table");
  const head = el("tr");
  for (const h of ["Step", "Job", "What", "Status", "Clock", "Assigned to"]) head.append(el("th", undefined, h));
  t.append(head);
  for (const s of d.steps) {
    const tr = el("tr");
    tr.append(
      el("td", undefined, String(s.no)), el("td", "num", s.number), el("td", undefined, s.what),
      el("td", undefined, s.status), el("td", "dim", s.clock), el("td", undefined, s.assignedTo),
    );
    t.append(tr);
  }
  if (d.steps.length === 0) t.append(fill(el("tr"), el("td", "dim", "no steps")));
  return t;
}

function linked(d: JobDetail, mayAmend: boolean): HTMLElement {
  const t = el("table");
  const head = el("tr");
  for (const h of ["Job", "Department", "What", "Status", "Assigned to", ""]) head.append(el("th", undefined, h));
  t.append(head);
  for (const l of d.links) {
    const tr = el("tr");
    tr.append(
      el("td", "num", l.number), el("td", undefined, l.department), el("td", undefined, l.what),
      fill(el("td"), status(l.status)), el("td", undefined, l.assignedTo),
      fill(el("td"), mayAmend ? control("btn sm", "Unlink") : null),
    );
    t.append(tr);
  }
  return t;
}
