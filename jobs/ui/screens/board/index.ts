/**
 * The board — mockup 01 frame 1: today's strip, the filters that are the
 * access model, twelve rows of the department's open jobs concern-first, and
 * the pager. Every row opens the job.
 */

import type { HostApi } from "@hotelos/sdk";

import { control, el, fill } from "../../chrome/element";
import { today as dayLine, when } from "../../chrome/instant";
import { concern, priority, status, tag } from "../../chrome/marks";
import { JOB_CREATE, JOB_READ } from "../../chrome/permissions";
import { standIn } from "../../chrome/standin";
import { pager } from "../../chrome/tabs";
import { load, may, type BoardPage, type JobRow, type Today } from "../../board";
import { recordedBoard, recordedToday } from "../../board/recorded/board";

/** What the board is told and tells back. */
export interface BoardPlace {
  filter: string;
  page: number;

  /** The job last opened from this board, which the row keeps marked. */
  opened: string | null;
  onFilter: (label: string) => void;
  onPage: (page: number) => void;
  onOpen: (jobId: string) => void;
  onRaise: () => void;
}

const FILTERS = ["My departments · ENG", "All departments", "Assigned to me", "Raised by guests", "Restricted", "Closed"];

/**
 * What a chip means to the service.
 *
 * The chips are the access model drawn (frame 1), and each is a filter the
 * board already supports rather than a word of its own. "Assigned to me" is
 * <code>mine</code> and carries no user id: whose jobs those are is the
 * caller's, resolved from the token, so a screen cannot filter to somebody
 * else's by editing a request.
 */
function asked(filter: string): Record<string, unknown> {
  switch (filter) {
    case "All departments": return {};
    case "Assigned to me": return { mine: true };
    case "Closed": return { statuses: ["RESOLVED", "CLOSED"] };
    case "Raised by guests": return { raisedKind: "GUEST" };
    case "Restricted": return { restricted: true };
    default: return { department: "ENG" };
  }
}

export async function board(host: HostApi, main: HTMLElement, place: BoardPlace): Promise<void> {
  const today = await load(host, JOB_READ, "today", recordedToday);
  const page = await load(host, JOB_READ, "board", recordedBoard, {
    ...asked(place.filter),
    // CORE-Q13's shape: the page asked for, and the size the service will
    // answer with — it applies its own ceiling and says which it used.
    page: place.page,
    pageSize: 12,
  });

  const body = el("div", "body");
  body.append(strip(host, today.value), filters(place, may(host, JOB_CREATE)), table(host, page.value.rows, place), pages(page.value, place));
  if (!page.live) body.append(standIn("board", page.because));
  main.replaceChildren(body);
}

function strip(host: HostApi, today: Today): HTMLElement {
  const line = el("div", "strip");
  const figure = (n: string, label: string): HTMLElement => fill(el("span"), el("b", undefined, n), label);
  line.append(
    figure(String(today.open), "open"), figure(String(today.breached), "breached"), figure(String(today.stuck), "stuck"),
    figure(String(today.running), "running"), figure(String(today.closedToday), "closed today"),
    figure(`${String(today.avgResolveMinutes)} min`, "avg to resolve"),
    // Department, day and time — the drawing's "ENG · Tue 2 Sep · 14:24".
    el("span", "end", `${today.department} · ${dayLine(host, today.at)}`),
  );
  return line;
}

function filters(place: BoardPlace, mayRaise: boolean): HTMLElement {
  const row = el("div", "chips");
  for (const label of FILTERS) {
    row.append(control(label === place.filter ? "chip on" : "chip", label, () => place.onFilter(label)));
  }
  if (mayRaise) fill(row, el("span", "grow"), control("btn pri", "＋ Raise a job", place.onRaise));
  return row;
}

function table(host: HostApi, rows: readonly JobRow[], place: BoardPlace): HTMLElement {
  const t = el("table");
  const head = el("tr");
  for (const h of ["Job", "Where", "What", "Pri", "Status", "Raised by", "Assigned to", "Concern", "Due"]) head.append(el("th", undefined, h));
  t.append(head);
  for (const row of rows) t.append(line(host, row, place));
  return t;
}

function line(host: HostApi, row: JobRow, place: BoardPlace): HTMLElement {
  const tr = el("tr", row.id === place.opened ? "pick sel" : "pick");
  tr.addEventListener("click", () => place.onOpen(row.id));
  const what = el("td", undefined, row.what);
  for (const t of row.tags) what.append(tag(t));
  tr.append(
    el("td", "num", row.number), el("td", undefined, row.where), what,
    fill(el("td"), priority(row.priority)), fill(el("td"), status(row.status)),
    el("td", undefined, row.raisedBy), el("td", undefined, row.assignedTo),
    fill(el("td"), row.concern === "ON_TRACK" && row.concernDetail !== null
      ? el("span", row.concernDetail === "clock stopped" ? "dim" : "pill", row.concernDetail)
      : concern(row.concern, row.concernDetail ?? undefined)),
    el("td", undefined, row.dueAt === null ? row.concernDetail ?? "—" : when(host, row.dueAt)),
  );
  return tr;
}

function pages(page: BoardPage, place: BoardPlace): HTMLElement {
  const { page: at, pageSize, total } = page.paging;
  const from = at * pageSize + 1;
  const to = Math.min(total, from + page.rows.length - 1);
  const count = Math.max(1, Math.ceil(total / pageSize));
  return pager(`${String(from)}–${String(to)} of ${String(total)} · ${String(pageSize)} per page at this height`, at, count, place.onPage);
}
