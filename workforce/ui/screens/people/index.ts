/**
 * People — postings, and the zone that makes a posting complete.
 *
 * # The zone is on the posting, not beside it
 *
 * `WF-Q7`. *"Anjali has zone 3"* is an incomplete fact; *"Anjali has zone 3 as
 * Front Office"* is complete — so the zone is drawn inside the posting's own
 * row, never as a column that could stand alone.
 */

import type { HostApi } from "@hotelos/sdk";

import { el } from "../../chrome/element";
import { ROSTER_READ } from "../../chrome/permissions";
import { load } from "../../roster";
import { recordedPeople, type People, type Posting } from "../../roster/people";

const COLUMNS = "1.5fr 116px 96px 1fr 1fr 116px";

/** Draw the screen. */
export async function people(host: HostApi, main: HTMLElement): Promise<void> {
  const got = await load(host, ROSTER_READ, "people", recordedPeople);
  const board = got.value;

  const body = el("div", "body");

  // Nobody posted is a real state with its own screen, not an empty table.
  body.append(board.postings.length === 0 ? firstRun() : table(board.postings));
  body.append(ownership());

  main.replaceChildren(header(board), body);
}

/** The header, counting what the list holds. */
function header(board: People): HTMLElement {
  const head = el("div", "head");
  const title = el("div");

  const here = board.postings.filter((p) => p.departments.includes("FO")).length;
  const expiring = board.postings.filter(
    (p) => p.tone === "warn" || p.tone === "bad").length;

  title.append(
    el("div", "ht", "People"),
    el("div", "hsub", board.postings.length === 0
      ? "Nobody is posted yet"
      : `${board.postings.length} posted · ${here} in Front Office · ${expiring} certifications expiring`),
  );

  const picker = el("div", "sel");
  picker.append(el("span", undefined, "All departments"), el("i", undefined, "▾"));

  const grow = el("div", "grow");
  head.append(title, picker, grow, el("div", "btn go", "＋ Post a staff member"));
  return head;
}

function table(postings: readonly Posting[]): HTMLElement {
  const list = el("div", "rows");

  const head = el("div", "row hd");
  head.style.gridTemplateColumns = COLUMNS;
  for (const label of ["Person", "Department", "Zone", "Job role", "Reports to", "Capability"]) {
    head.append(el("div", undefined, label));
  }
  list.append(head);

  for (const posting of postings) {
    list.append(row(posting));
  }

  return list;
}

function row(posting: Posting): HTMLElement {
  const item = el("div", "row");
  item.style.gridTemplateColumns = COLUMNS;

  const who = el("div");
  const name = el("div", "wn");
  name.append(el("span", undefined, posting.who));

  if (posting.reportsTo.startsWith("—")) {
    name.append(el("em", undefined, "head"));
  }

  who.append(name, el("s", undefined, posting.since));

  const departments = el("div", "deps");
  for (const code of posting.departments) {
    departments.append(el("span", "pill neu", code));
  }

  item.append(
    who,
    departments,
    zone(posting.zone),
    el("div", undefined, posting.role),
    el("div", "dim", posting.reportsTo),
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
    el("div", "btn go", "＋ Post a staff member"),
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
  cell.append(value === null ? el("span", "dim", "—") : el("span", "pill acc", value));
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
