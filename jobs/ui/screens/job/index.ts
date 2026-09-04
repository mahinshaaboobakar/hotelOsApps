/**
 * One job — mockup 01 frames 2 to 2g: the header is always there (number,
 * title, priority, status, concern, the live timer, one line of who and when,
 * the action row); seven tabs beneath it hold one kind of thing each.
 */

import type { HostApi } from "@hotelos/sdk";

import { control, el, fill } from "../../chrome/element";
import { elapsed, sinceSeconds } from "../../chrome/instant";
import { concern, priority, status } from "../../chrome/marks";
import { JOB_AMEND, JOB_ASSIGN, JOB_CANCEL, JOB_COMPLETE, JOB_READ } from "../../chrome/permissions";
import { standIn } from "../../chrome/standin";
import { subnav, type Tab } from "../../chrome/tabs";
import { load, may, type JobDetail } from "../../board";
import { recordedJob, recordedRatedJob } from "../../board/recorded/job";
import { history } from "./history";
import { links } from "./links";
import { notes } from "./notes";
import { overview } from "./overview";
import { rating } from "./rating";
import { record } from "./record";
import { work } from "./work";

/** What the job view is told and tells back. */
export interface JobPlace {
  jobId: string;
  tab: string;
  onTab: (label: string) => void;
  onResolve: () => void;
  onBack: () => void;
}

export async function job(host: HostApi, main: HTMLElement, place: JobPlace): Promise<void> {
  const recorded = place.jobId === "j388" ? recordedRatedJob : recordedJob;
  const got = await load(host, JOB_READ, "job", recorded, { id: place.jobId });
  const detail = got.value;
  const body = el("div", "body");
  body.append(header(host, detail, place), subnav(tabs(detail), place.tab, place.onTab), tab(host, detail, place));
  if (!got.live) body.append(standIn("job", got.because));
  main.replaceChildren(body);
}

function tabs(d: JobDetail): readonly Tab[] {
  const list: Tab[] = [
    { label: "Overview" },
    { label: "Work", count: String(d.sessions.length) },
    { label: "History", count: String(d.history.length) },
    { label: "Notes & photos", count: String(d.notes.length) },
    { label: "Links & steps", count: String(d.links.length + d.steps.length) },
  ];
  if (d.row.raisedBy.startsWith("Guest")) list.push({ label: "Rating" });
  list.push({ label: "Record" });
  return list;
}

function tab(host: HostApi, d: JobDetail, place: JobPlace): HTMLElement {
  switch (place.tab) {
    case "Work": return work(host, d, may(host, JOB_COMPLETE), place.onResolve);
    case "History": return history(host, d);
    case "Notes & photos": return notes(host, d);
    case "Links & steps": return links(d, may(host, JOB_AMEND));
    case "Rating": return rating(host, d);
    case "Record": return record(d);
    default: return overview(d);
  }
}

function header(host: HostApi, d: JobDetail, place: JobPlace): HTMLElement {
  const top = el("div", "row");
  top.append(
    control("btn sm", "‹ Board", place.onBack),
    el("span", "num", d.row.number), el("span", "title", `${d.row.what} — ${d.row.where}`),
    priority(d.row.priority), status(d.row.status),
    concern(d.row.concern, d.row.concernDetail === null ? undefined : `${d.row.concernDetail} over`),
  );
  if (d.runningSince !== null) {
    const timer = fill(el("span", "timer grow"), el("i"), elapsed(sinceSeconds(d.runningSince)));
    top.append(timer);
  }

  const line = el("div", "mono", `${d.runningWho === null ? "" : `${d.runningWho} working · `}${d.raisedLine} · accountable now ${d.accountable}`);
  return fill(el("div"), top, line, actions(host, d, place));
}

/** The action row is the permission set (design §4.1); a viewer with none sees no row. */
function actions(host: HostApi, d: JobDetail, place: JobPlace): HTMLElement | null {
  if (d.row.status === "CLOSED" || d.row.status === "CANCELLED") return null;
  const row = el("div", "row");
  if (d.runningWho !== null) row.append(control("btn", "Pause"), control("btn", "Stop"));
  if (may(host, JOB_COMPLETE)) row.append(control("btn pri", "Resolve…", place.onResolve));
  if (may(host, JOB_AMEND)) row.append(control("btn", "Put on hold…"));
  if (may(host, JOB_ASSIGN)) row.append(control("btn", "Reassign…"));
  if (may(host, JOB_AMEND) || may(host, JOB_CANCEL)) row.append(control("btn", "More ▾"));
  return row.childElementCount === 0 ? null : row;
}
