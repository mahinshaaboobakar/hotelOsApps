/**
 * People — postings, and the zone that makes a posting complete.
 *
 * # The zone is on the posting, not beside it
 *
 * `WF-Q7`. *"Anjali has zone 3"* is an incomplete fact; *"Anjali has zone 3 as
 * Front Office"* is complete — so the zone is drawn inside the posting's own
 * row, never as a column that could stand alone.
 */

import {
  formatDay, formatNumber, type HostApi, load, type PropertyEnvironment, type ReadFailure,
} from "@hotelos/sdk";

import { allDepartments } from "../../chrome/department";
import { el, unavailable } from "../../chrome/element";
import { overlay } from "../../chrome/overlay";
import { ROSTER_READ } from "../../chrome/permissions";
import { failureScreen } from "../../chrome/failure";
import { type People, type Posting } from "../../roster/people";
import { endPosting } from "./end-posting";
import { pager } from "../../chrome/pager";
import type { PostingEnding } from "../../roster/team";
import { failureBody } from "../../chrome/failure";

const COLUMNS = "1.5fr 116px 96px 1fr 1fr 116px";

/**
 * Draw the screen.
 *
 * @param host the bridge, and the only route out of this realm
 * @param main where the screen mounts
 * @param ending whether the end-posting dialog is open over it
 * @param close dismiss the dialog
 * @param onEnd open the dialog on one posting, by its id
 * @param onPage turn to a page, 0-based
 * @param page which page to ask for, 0-based
 */
export async function people(
  host: HostApi,
  main: HTMLElement,
  ending: string | null = null,
  close: () => void = () => {},
  onEnd: (posting: string) => void = () => {},
  onPage: (page: number) => void = () => {},
  page = 0,
): Promise<void> {
  // The page is part of the QUESTION, not something the screen slices off the
  // answer. A screen that fetched everything and cut it locally would be a
  // pager over a list the property already sent in full, which is the thing
  // paging exists to avoid.
  const got = await load<People>(host, ROSTER_READ, "people", { page });

  // No fallback - `APPS-Q26(4)`. The page is carried into the retry: a retry
  // that dropped it would move a person to page one and call it a retry.
  if (!got.ok) {
    failureScreen(main, "People", got.failure, { the: "the people here" }, host.property,
      () => void people(host, main, ending, close, onEnd, onPage, page));
    return;
  }

  const board = got.value;

  const body = el("div", "body");

  // Nobody posted is a real state with its own screen, not an empty table —
  // **and "nobody posted" is the list's TOTAL, counted by the service where the
  // rows live, never this page's rows.** This read `board.postings.length`, so
  // page 3 of a 42-person list drew "Post your first staff member" (app surface
  // audit, 2026-09-19, G5/G7): a count taken from a capped read measures the
  // page and calls it the property. An empty page of a non-empty list is an
  // ordinary list saying it has nothing here.
  body.append(board.paging.total === 0 ? firstRun() : table(board.postings, onEnd, host.property));

  // No ownership panel. It read "Identity is Master Data's and read-only here.
  // … This screen owns what is operational" — which system owns which field is
  // a note for the developer, never UI (owner ruling, 2026-09-19).

  // **Inside the body, as the list's floor** — §6 as ruled 2026-09-05.
  //
  // This was a sibling of the body, pinned below the scroll, which kept it in
  // view and cost the strip its place in the list. The ruled treatment gets the
  // same outcome from the list growing and the strip sticking, and it is the
  // one every application now draws: `.rows:has(~ .pager)` needs the two to be
  // siblings, which is what putting it here is for.
  const pages = pager(board.paging, board.postings.length, host.property, onPage);
  if (pages !== null) body.append(pages);

  main.replaceChildren(header(board, ending, host.property), body);

  // Ending a posting closes team memberships with it, and until this dialog
  // existed nothing said so — the round's finding, drawn.
  //
  // **Read, not built here.** The consequence panel is the service's answer to
  // `roster.read/ending`; this screen used to compose it, and for everybody
  // except one recorded person it composed an empty list — the panel that
  // exists to say what else closes, saying nothing, before a destructive
  // button.
  if (ending !== null) {
    const got = await load<PostingEnding>(host, ROSTER_READ, "ending", { posting: ending });

    main.append(got.ok
      ? endPosting(host, close, got.value, () => { close(); })
      // A dialog that cannot read what it is about does not open a destructive
      // button over a guess. It says what it could not read, and offers the way
      // out — the same rule the screens follow, one surface in.
      : cannotRead(got.failure, host.property, close));
  }
}


/**
 * The header, counting what the list holds.
 *
 * @param board the postings
 * @param ending whose posting is being ended, when one is — the subtitle names
 *   them, because a dialog over a dimmed table needs the page to say who it is
 *   about
 * @param property whose locale the counts are written in
 * @returns the header
 */
function header(
  board: People, ending: string | null, property: PropertyEnvironment,
): HTMLElement {
  const head = el("div", "tools");
  const title = el("div");

  title.append(el("div", "hsub", subtitle(board, ending, property)));

  const grow = el("div", "grow");
  head.append(title, allDepartments(), grow,
    unavailable("btn pri", "＋ Post a staff member", "Postings cannot be made here yet."));
  return head;
}

/** What the header says under the title. */
function subtitle(
  board: People, ending: string | null, property: PropertyEnvironment,
): string {
  const n = (value: number): string => formatNumber(value, property, "whole");

  if (ending !== null) {
    // Found by id, because that is what the click carried. It was found by
    // name, so a subtitle could name the wrong person's departments the day a
    // property employed two people called the same thing.
    const posting = board.postings.find((one) => one.id === ending);
    if (posting === undefined) return "";

    return `${posting.who} · ${posting.departments.join(" · ")}`.trim();
  }

  // **Every figure is the property's, counted by the service where the rows
  // live** (app surface audit, 2026-09-19). Three were taken from this page:
  // "nobody posted" from its length, so an empty page 3 of forty people said
  // nobody was; a Front Office count, a department code written into the
  // screen for every property; and "certifications expiring" from the rows'
  // tone, which counted people rather than certificates and expired as
  // expiring.
  return board.paging.total === 0
    ? "Nobody is posted yet"
    : `${n(board.paging.total)} posted · ${n(board.expiring)} certifications expiring`;
}

function table(
  postings: readonly Posting[], onEnd: (posting: string) => void,
  property: PropertyEnvironment,
): HTMLElement {
  const list = el("div", "rows");

  const head = el("div", "row hd");
  head.style.gridTemplateColumns = COLUMNS;
  for (const label of ["Person", "Department", "Zone", "Job role", "Reports to", "Capability"]) {
    head.append(el("div", undefined, label));
  }
  list.append(head);

  for (const posting of postings) {
    list.append(row(posting, onEnd, property));
  }

  // A page with no rows says so in the list — the owner's direction for `64f`,
  // *"if no data, need to show that in screen"*. The words are §6's own for an
  // empty page until `64f` rules what an empty list says.
  if (postings.length === 0) list.append(el("div", "none", "No rows on this page."));

  return list;
}

/**
 * The dialog, when the service could not say what ending this posting does.
 *
 * **No destructive button over a guess.** The screen used to compose the
 * consequence itself and would have shown an empty one; a dialog that cannot
 * read what it is about says what it could not read and offers the way out,
 * which is the same rule the screens follow one surface further in.
 *
 * @param failure what the read reported
 * @param property the locale and zone the facts are read in
 * @param close the way out
 * @returns the overlay
 */
function cannotRead(
  failure: ReadFailure, property: PropertyEnvironment, close: () => void,
): HTMLElement {
  const acts = el("div", "acts");
  const cancel = el("button", "btn", "Close");
  cancel.setAttribute("type", "button");
  cancel.addEventListener("click", close);
  acts.append(el("div", "grow"), cancel);

  // A dialog: nothing is being composed, a failure is being acknowledged (§9).
  // The failure body carries its own heading, so the head is empty.
  return overlay("dialog", {
    head: [],
    body: [failureBody(failure, { the: "what ending this posting would close" }, property)],
    foot: [acts],
  }, close);
}

/**
 * One posting.
 *
 * **A button, because it opens something.** The locked frame draws the table
 * blurred behind the dialog and so does not say what was clicked; a row that
 * opens the posting it names is the module's existing idiom (the rota's cells
 * and the teams list both work this way), and it is recorded as an
 * implementation choice rather than read off the drawing.
 */
function row(
  posting: Posting, onEnd: (posting: string) => void, property: PropertyEnvironment,
): HTMLElement {
  const item = el("button", "row");
  item.setAttribute("type", "button");
  item.style.gridTemplateColumns = COLUMNS;
  // **The posting's id, not the person's name.** The name is what a
  // row shows; it is not what identifies a posting, and two people of
  // one name were one row to everything downstream of this click.
  item.addEventListener("click", () => { onEnd(posting.id); });

  const who = el("div");
  const name = el("div", "wn");
  name.append(el("span", undefined, posting.who));

  if (posting.reportsTo.startsWith("—")) {
    name.append(el("em", undefined, "★ head"));
  }

  // **The word and the count are the screen's; the date is the wire's.**
  // The service sent this whole line as one string, so the date could not
  // be read without the sentence around it and the month name came from
  // the account the service runs under.
  const since = `Since ${formatDay(posting.since, property, "day-month-year")}`;

  who.append(name, el("s", undefined,
    posting.postings > 1
      ? `${since} · ${formatNumber(posting.postings, property, "whole")} postings`
      : since));

  const departments = el("div", "deps");
  for (const code of posting.departments) {
    departments.append(el("span", "pill neu", code));
  }

  item.append(
    who,
    departments,
    zone(posting.zone),
    el("div", undefined, posting.role),
    el("div", "quiet", posting.reportsTo),
    el("div", `pill ${posting.tone}`, standing(posting, property)),
  );

  return item;
}

/**
 * The first run — what a property sees before anybody is posted.
 *
 * **It names the consequence rather than the button.** A posting is not
 * paperwork: until one exists, the rota, leave, the duty register and
 * attendance have nobody to be about, and `department#posted` resolves to
 * nobody so every department-scoped document grant in My Hotel is dormant.
 */
function firstRun(): HTMLElement {
  const panel = el("div", "first");

  panel.append(
    el("div", "fmark", "◎"),
    el("div", "ft", "Post your first staff member"),
    el("div", "note",
      "A posting says where a person works and as what. Everything else in "
      + "Workforce is built on it — the rota, leave, the duty roster and "
      + "attendance all need somebody posted first."),
    // A second paragraph explained that postings resolve My Hotel's department
    // document access — how two systems connect, a developer's note (owner
    // ruling, 2026-09-19).
    unavailable("btn pri", "＋ Post a staff member", "Postings cannot be made here yet."),
  );

  return panel;
}

/**
 * The zone, as a chip.
 *
 * It reads as an assignment rather than as a description, which is what it is:
 * a standing arrangement on the posting, not an attribute of the person.
 */
function zone(value: string | null): HTMLElement {
  const cell = el("div");
  cell.append(value === null ? el("span", "quiet", "—") : el("span", "pill acc", value));
  return cell;
}

/**
 * A row's standing in words — the service sends the band and the count.
 *
 * The words are the screen's and the digits the property's (NUM-Q1, ADR
 * 0174); the service used to send `"2 expiring"` whole.
 */
function standing(posting: Posting, property: PropertyEnvironment): string {
  if (posting.standing === "none") return "none recorded";
  return `${formatNumber(posting.certificates, property, "whole")} ${posting.standing}`;
}

