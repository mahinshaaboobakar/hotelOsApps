import { formatNumber, type PropertyEnvironment } from "@hotelos/sdk";

/**
 * The Links & steps tab — frame 2e: the job's steps in sequence, then its
 * group links. Two relations, drawn apart because they mean different things
 * (S1 D2).
 */

import { el, fill, off } from "../../chrome/element";
import { status } from "../../chrome/marks";
import type { JobDetail } from "../../board";

const LATER = "linking and adding steps aren't available here yet";

export function links(d: JobDetail, mayAmend: boolean, property: PropertyEnvironment): HTMLElement {
  const root = el("div");
  root.append(el("div", "sect", "Steps of this job · sequence"), steps(d, property), el("div", "mono", "Cancelling this job cancels its steps. Closing it never closes them."));
  root.append(el("div", "sect", "Linked jobs · same room, related"), linked(d, mayAmend));
  if (mayAmend) {
    const row = el("div", "row");
    // The backend answers `link`, but choosing the job to link needs a picker
    // no frame draws, and adding a step or unlinking has no operation at all.
    // Drawn off until each is built (owner, 2026-09-19: nothing live and inert).
    row.append(off("btn", "Link a job…", LATER), off("btn", "Add a step…", LATER), el("span", "mono", LATER));
    root.append(row);
  }
  return root;
}

function steps(d: JobDetail, property: PropertyEnvironment): HTMLElement {
  const t = el("table");
  const head = el("tr");
  for (const h of ["Step", "Job", "What", "Status", "Clock", "Assigned to"]) head.append(el("th", undefined, h));
  t.append(head);
  for (const s of d.steps) {
    const tr = el("tr");
    tr.append(
      el("td", undefined, formatNumber(s.no, property)), el("td", "num", s.number), el("td", undefined, s.what),
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
      fill(el("td"), mayAmend ? off("btn sm", "Unlink", LATER) : null),
    );
    t.append(tr);
  }
  return t;
}
