/**
 * The board — mockup 01 frame 1: today's strip, the filters that are the
 * access model, twelve rows of the department's open jobs concern-first, and
 * the pager. Every row opens the job.
 */

import { formatNumber, load, type HostApi } from "@hotelos/sdk";

import { control, el, fill } from "../../chrome/element";
import { today as dayLine, when } from "../../chrome/instant";
import { concern, priority, status, tag } from "../../chrome/marks";
import { JOB_CREATE, JOB_READ } from "../../chrome/permissions";
import { failure, failureState } from "../../chrome/failure";
import { pager } from "../../chrome/tabs";
import { may, type BoardPage, type JobRow, type Today } from "../../board";

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

// **"My departments" names no department.** It used to read "My departments ·
// ENG", which told the person their departments were Engineering — an
// attribution about them that this application has never established. Jobs
// cannot resolve a person's postings: they are Workforce's, and there is no
// client (design §6). So the tab keeps its place on the locked frame, asks for
// the caller's own departments, and the screen says plainly when nothing can
// answer that — rather than quietly showing one hotel department's work as
// though it were theirs.
const FILTERS = ["My departments", "All departments", "Assigned to me", "Raised by guests", "Restricted", "Closed"];

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
    default: return { mineDepartments: true };
  }
}

export async function board(host: HostApi, main: HTMLElement, place: BoardPlace): Promise<void> {
  const today = await load<Today>(host, JOB_READ, "today");
  const page = await load<BoardPage>(host, JOB_READ, "board", {
    ...asked(place.filter),
    // CORE-Q13's shape: the page asked for, and the size the service will
    // answer with — it applies its own ceiling and says which it used.
    page: place.page,
    pageSize: 12,
  });

  // **The board either shows this property's jobs or says why it cannot.** It
  // used to draw the approved example under a banner; the owner ruled that out
  // on 2026-09-09 and the seam now makes it unwriteable — there is no fallback
  // to pass. The strip is a second read, so a page that arrived with a strip
  // that did not still draws the board and says what is missing above it.
  if (!page.ok) {
    // "this board" is 64b's own noun — the frame draws Jobs' Board failing as
    // "Jobs could not build this board", and the noun is the one word of the
    // sentence this module supplies rather than the SDK.
    main.replaceChildren(failure(host.property, page.failure, "this board", () => void board(host, main, place)));
    return;
  }

  const body = el("div", "body");

  body.append(
    today.ok
      ? strip(host, today.value)
      // The strip failing inside a board that did render is a placement 64b
      // does not draw — it has a screen size and a widget size. The state block
      // without the screen's centring is the least that is not invented, and
      // it is reported as undrawn in chapter 05 rather than treated as settled.
      : failureState(host.property, today.failure, "today's figures", () => void board(host, main, place)),
    filters(place, may(host, JOB_CREATE)),
    // The list is a wrapper round the table: `.tbl` is what grows and scrolls
    // (standard §6, CORE-Q28), because a table cannot be a scroll container.
    fill(el("div", "tbl"), table(host, page.value.rows, place)),
    pages(page.value, place, host.property),
  );

  main.replaceChildren(body);
}

function strip(host: HostApi, today: Today): HTMLElement {
  const line = el("div", "strip");
  // Every figure through formatNumber, in the property's locale — §12 (U1).
  const n = (value: number): string => formatNumber(value, host.property);
  const figure = (value: string, label: string): HTMLElement => fill(el("span"), el("b", undefined, value), label);
  line.append(
    figure(n(today.open), "open"), figure(n(today.breached), "breached"), figure(n(today.stuck), "stuck"),
    figure(n(today.running), "running"), figure(n(today.closedToday), "closed today"),
    figure(`${n(today.avgResolveMinutes)} min`, "avg to resolve"),
    // Department, day and time — the drawing's "ENG · Tue 2 Sep · 14:24" (en-GB, Asia/Qatar).
    el("span", "end", `${today.department} · ${dayLine(host, today.at)}`),
  );
  return line;
}

/**
 * "My departments" is drawn off, with its reason, because nothing can answer it:
 * the backend returns no rows for it (`JobQueries.cs`, MineDepartmentsOnly),
 * since which departments a person belongs to is not known to Jobs until
 * ADR 0203. It was drawn live and selected, over an empty list and a paragraph
 * explaining why — developer content on a screen (owner ruling, 2026-09-19).
 * Off, the screen says it in its own words and opens on All departments.
 */
const UNKNOWN_DEPARTMENTS = "your departments aren't known yet";

function filters(place: BoardPlace, mayRaise: boolean): HTMLElement {
  const row = el("div", "chips");
  for (const label of FILTERS) {
    if (label === "My departments") {
      const off = el("button", "btn chip off", label) as HTMLButtonElement;
      off.disabled = true;
      off.title = UNKNOWN_DEPARTMENTS;
      row.append(off);
      continue;
    }
    row.append(control(label === place.filter ? "btn chip on" : "btn chip", label, () => place.onFilter(label)));
  }
  row.append(el("span", "mono", `My departments: ${UNKNOWN_DEPARTMENTS}`));
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
  // The opener button handles its own click; without this the click would
  // bubble here and open the job twice.
  tr.addEventListener("click", (event) => {
    if ((event.target as Element | null)?.closest(".opener")) return;
    place.onOpen(row.id);
  });
  const what = el("td", undefined, row.what);
  for (const t of row.tags) what.append(tag(t));
  tr.append(
    // The number is the row's opener, and a real button — standard §2 (checklist
    // C8): the row opened on a click no keyboard could reach. The row keeps its
    // click for a pointer; the button is what Tab lands on.
    fill(el("td", "num"), control("opener", row.number, () => place.onOpen(row.id))), el("td", undefined, row.where), what,
    fill(el("td"), priority(row.priority)), fill(el("td"), status(row.status)),
    el("td", undefined, row.raisedBy), el("td", undefined, row.assignedTo),
    fill(el("td"), row.concern === "ON_TRACK" && row.concernDetail !== null
      ? el("span", row.concernDetail === "clock stopped" ? "dim" : "pill", row.concernDetail)
      : concern(row.concern, row.concernDetail ?? undefined)),
    el("td", undefined, row.dueAt === null ? row.concernDetail ?? "—" : when(host, row.dueAt)),
  );
  return tr;
}

function pages(page: BoardPage, place: BoardPlace, property: HostApi["property"]): HTMLElement {
  // The SDK's pager (`chrome/tabs.ts`, checklist G2): the range from the rows
  // that arrived, an empty page kept apart from an empty list. The page size is
  // the Board's own addition, drawn only after a range — it explains a count,
  // and an empty state has none to explain.
  return pager(page.paging, page.rows.length, place.onPage, property, `${formatNumber(page.paging.pageSize, property)} per page at this height`);
}
