/**
 * People — postings, and the zone that makes a posting complete.
 *
 * # The zone is on the posting, not beside it
 *
 * `WF-Q7`. *"Anjali has zone 3"* is an incomplete fact; *"Anjali has zone 3 as
 * Front Office"* is complete — so the zone is drawn inside the posting's own
 * row, never as a column that could stand alone.
 */

import { formatDay, type HostApi, load, type PropertyEnvironment, type ReadFailure }
  from "@hotelos/sdk";

import { el } from "../../chrome/element";
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

  // Nobody posted is a real state with its own screen, not an empty table.
  body.append(board.postings.length === 0 ? firstRun() : table(board.postings, onEnd, host.property));

  body.append(ownership());

  // **Inside the body, as the list's floor** — §6 as ruled 2026-09-05.
  //
  // This was a sibling of the body, pinned below the scroll, which kept it in
  // view and cost the strip its place in the list. The ruled treatment gets the
  // same outcome from the list growing and the strip sticking, and it is the
  // one every application now draws: `.rows:has(~ .pager)` needs the two to be
  // siblings, which is what putting it here is for.
  const pages = pager(board.paging, board.postings.length, onPage);
  if (pages !== null) body.append(pages);

  main.replaceChildren(header(board, ending), body);

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
 * @returns the header
 */
function header(board: People, ending: string | null): HTMLElement {
  const head = el("div", "tools");
  const title = el("div");

  const here = board.postings.filter((p) => p.departments.includes("FO")).length;
  const expiring = board.postings.filter(
    (p) => p.tone === "warn" || p.tone === "bad").length;

  title.append(
    el("div", "hsub", subtitle(board, ending, here, expiring)),
  );

  const picker = el("div", "sel");
  picker.append(el("span", undefined, "All departments"), el("i", undefined, "▾"));

  const grow = el("div", "grow");
  head.append(title, picker, grow, el("div", "btn pri", "＋ Post a staff member"));
  return head;
}

/** What the header says under the title. */
function subtitle(
  board: People, ending: string | null, here: number, expiring: number,
): string {
  if (ending !== null) {
    // Found by id, because that is what the click carried. It was found by
    // name, so a subtitle could name the wrong person's departments the day a
    // property employed two people called the same thing.
    const posting = board.postings.find((one) => one.id === ending);
    if (posting === undefined) return "";

    return `${posting.who} · ${posting.departments.join(" · ")}`.trim();
  }

  // **The property's total, not this page's length.** A subtitle counting the
  // rows in front of you under a list that pages says something false about the
  // property the moment somebody turns to page two.
  return board.postings.length === 0
    ? "Nobody is posted yet"
    : `${board.paging.total} posted · ${here} in Front Office on this page · `
      + `${expiring} certifications expiring`;
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
  const scrim = el("div", "scrim");
  const dialog = el("div", "dlg");

  dialog.append(
    failureBody(failure, { the: "what ending this posting would close" }, property));

  const acts = el("div", "acts");
  const cancel = el("button", "btn", "Close");
  cancel.setAttribute("type", "button");
  cancel.addEventListener("click", close);
  acts.append(el("div", "grow"), cancel);
  dialog.append(acts);

  scrim.append(dialog);
  scrim.addEventListener("click", (event) => {
    if (event.target === scrim) close();
  });

  return scrim;
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
    posting.postings > 1 ? `${since} · ${posting.postings} postings` : since));

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
    el("div", `pill ${posting.tone}`, posting.capability),
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
    el("div", "note",
      "It also opens the department folders in My Hotel: until a property has "
      + "postings, department-based document access has nobody to resolve to."),
    el("div", "btn pri", "＋ Post a staff member"),
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

/** What this screen owns, and what it does not. */
function ownership(): HTMLElement {
  const panel = el("div", "panel");
  const note = el("div", "note");

  note.append(
    el("b", undefined, "Identity is Master Data's and read-only here. "),
    el("span", undefined,
      "Name, employee number, contact and photograph belong to the person and are "
      + "edited in Core Administration. This screen owns what is operational — the "
      + "posting, the job role, the reporting line, the zone and the department head."),
  );

  panel.append(note);
  return panel;
}
