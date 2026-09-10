/**
 * Teams — a named group of posted staff in one department, formed to be
 * assigned work.
 *
 * # The screen that should have come first
 *
 * The object was ruled and built from another application's request, and went in
 * without a drawing; the seven frames were drawn afterwards and locked on
 * 2026-09-04. This builds to them. The order was backwards once and the page
 * says so — it is not a habit.
 *
 * # A team is people, and a zone is a place
 *
 * Both exist and a hotel will use the words interchangeably. The screen keeps
 * them apart by never showing one where the other belongs: a team's row carries
 * its **department**, never a zone, and the zone stays on the posting where
 * `WF-Q7` put it.
 */

import { formatDay, formatInstant, type HostApi, type PropertyEnvironment }
  from "@hotelos/sdk";

import { el, fill } from "../../chrome/element";
import { ROSTER_READ } from "../../chrome/permissions";
import { failureScreen } from "../../chrome/failure";
import { load } from "../../roster";
import type { Team, TeamDetail, Teams } from "../../roster/team";

import { detail } from "./detail";

/** What a screen needs from the module to open a dialog over itself. */
export interface TeamPlace {
  /** Which dialog is open, when one is. */
  dialog: string | null;

  /** Open one. */
  open: (what: string) => void;

  /** Dismiss it. */
  close: () => void;

  /** Which team the detail pane is open on, when one is. */
  team: string | null;

  /** Open one. */
  onTeam: (id: string) => void;
}

/**
 * Draw the screen.
 *
 * @param host the bridge, and the only route out of this realm
 * @param main where the screen mounts
 * @param place which dialog is open, and how to open or close one
 */
export async function teams(
  host: HostApi,
  main: HTMLElement,
  place: TeamPlace = {
    dialog: null, open: () => {}, close: () => {}, team: null, onTeam: () => {},
  },
): Promise<void> {
  // Frame 7 is a DATA state, not a route: a property that has formed none gets
  // the same screen, answered with none. A flag would make "empty" reachable
  // with teams in the answer, which is a state nobody drew.
  const got = await load<Teams>(host, ROSTER_READ, "teams");

  if (!got.ok) {
    failureScreen(main, "People", got.failure, { the: "this property's teams" },
      () => void teams(host, main, place));
    return;
  }

  const board = got.value;

  // Open only when the list has selected a team AND the answer carries its
  // roll. Either alone draws the list, which is the truthful half.
  const open = board.detail !== null && board.detail.team.id === place.team
    ? board.detail
    : null;

  const body = el("div", "body");

  // Having formed none is a real state with its own frame, not an empty table.
  if (board.teams.length === 0) {
    body.append(firstRun(place));
  } else if (open === null) {
    body.append(list(board, place, host.property), oneDepartment());
  } else {
    body.append(split(board, open, place, host.property));
  }

  main.replaceChildren(header(board, place, open, host.property), body);

  if (place.dialog !== null) {
    // `board` and `host` reach the overlays because a write needs both: the
    // sheet sends through the same seam every read uses, and it checks the
    // name against the property's own teams rather than against a literal.
    //
    // `done` is `close`, and that is the refresh: closing a dialog redraws the
    // screen, and the redraw re-runs the read at the top of this function. A
    // separate refresh call would be a second way to reach the same state.
    const overlay = await overlays(host, board, place, open,
      () => { place.close(); });
    if (overlay !== null) main.append(overlay);
  }
}

/**
 * The dialogs this screen opens over itself.
 *
 * @param host the bridge, for the sheets that write
 * @param board the answer this screen drew, for the facts a sheet checks
 * @param place which one is open, and how to dismiss it
 * @param open the team the detail pane has, when it has one
 * @param done called after a write lands, so the screen re-reads
 * @returns the overlay, or nothing
 */
async function overlays(
  host: HostApi,
  board: Teams,
  place: TeamPlace,
  open: TeamDetail | null,
  done: () => void,
): Promise<HTMLElement | null> {
  if (place.dialog === "form") {
    return (await import("./form")).formTeam(
      host, board.departments, board.teams, place.close, done);
  }

  if (place.dialog === "member") {
    // The team the pane has open, which this function already holds. The
    // dialog used to read `recordedTeams.detail` instead - a fixture, on a
    // live screen, naming a team and its candidates.
    //
    // `onDate` is the board's own day, so the date the dialog shows and the
    // date the write sends are one value read once.
    return (await import("./member")).addMember(
      host, board.onDate, place.close, open, done);
  }

  // Acts on the team the pane has open, so it does not exist without one —
  // which is also why the control that opens it lives in the pane.
  if (place.dialog === "down" && open !== null) {
    return (await import("./stand-down")).standDown(place.close, open);
  }

  return null;
}

/**
 * The header — the property, and what the list holds.
 *
 * @param board the teams
 * @param place how to open a dialog
 * @param open the team the detail pane is showing, when one is
 * @returns the header
 */
function header(
  board: Teams, place: TeamPlace, open: TeamDetail | null,
  property: PropertyEnvironment,
): HTMLElement {
  const head = el("div", "tools");
  const title = el("div");

  // **The count is what the list holds**, stood-down rows included, because the
  // subtitle sits above the list and a person reads them together. Counting
  // only the live ones made the header say four over five rows.
  const departments = new Set(board.teams.map((team) => team.department)).size;

  title.append(
    el("div", "hsub", board.teams.length === 0
      ? board.property
      : `${board.property} · ${board.teams.length} teams in ${departments} departments`),
  );

  const grow = el("div", "grow");
  const form = el("button", "btn pri", "＋ Form a team");
  form.setAttribute("type", "button");
  form.addEventListener("click", () => { place.open("form"); });

  // The detail pane asks about a day, so the day belongs in the header beside
  // it — the same shape the rota and attendance headers already use.
  if (open !== null) {
    head.append(title, grow, el("div", "btn",
      // `day-month-year`, not `weekday-day`. The SDK publishes no style that
      // renders weekday-day-MONTH, which is what this chip and the pane's
      // "Members on ..." actually want - a supervisor paging days needs the
      // weekday, and a strip that says "Fri 04" says nothing when the paging
      // crosses a month. Reported rather than composed here: building the
      // string myself would put culture-sensitive formatting straight back
      // into this module, which is the thing ADR 0152 just removed.
      `‹ ${formatDay(board.onDate, property, "day-month-year")} ›`), form);
    return head;
  }

  const picker = el("div", "sel");
  picker.append(el("span", undefined, "All departments"), el("i", undefined, "▾"));

  return fill(head, title, board.teams.length === 0 ? null : picker, grow,
    board.teams.length === 0 ? null : el("div", "btn", "Show stood down"), form);
}

/**
 * Frame 1 — every team, with the stood-down one drawn quietly.
 *
 * @param board the teams
 * @param place how to open one
 * @returns the card
 */
function list(
  board: Teams, place: TeamPlace, property: PropertyEnvironment,
): HTMLElement {
  // A list sits bare on the page — no wrapper, no fill, no radius. A card is
  // for a thing you are looking at; a row is for one of many you are looking
  // through, and the difference is how many fit on a screen.
  const card = el("div", "list");
  const head = el("div", "tgrid hd");

  head.append(
    el("div", undefined, "Team"), el("div", undefined, "Department"),
    el("div", undefined, "Members"), el("div", undefined, "Formed"),
    el("div", undefined, "Status"));

  card.append(head);

  for (const team of board.teams) {
    card.append(row(team, false, "tgrid", board, place, property));
  }

  return card;
}

/**
 * Frame 2 — the list beside the team that is open.
 *
 * @param board the teams
 * @param open the one whose roll the answer carries
 * @param place how to open another
 * @returns the two columns
 */
function split(
  board: Teams, open: TeamDetail, place: TeamPlace,
  property: PropertyEnvironment,
): HTMLElement {
  const columns = el("div", "tsplit");
  // A list sits bare on the page — no wrapper, no fill, no radius. A card is
  // for a thing you are looking at; a row is for one of many you are looking
  // through, and the difference is how many fit on a screen.
  const card = el("div", "list");
  const head = el("div", "tnarrow hd");

  head.append(
    el("div", undefined, "Team"), el("div", undefined, "Dept"),
    el("div", undefined, "Members"));

  card.append(head);

  for (const team of board.teams) {
    card.append(
      row(team, open.team.id === team.id, "tnarrow", board, place, property));
  }

  columns.append(card, detail(open, place, property));
  return columns;
}

/**
 * One team's row, in either width.
 *
 * The two grids differ in what they can afford to show, not in what a row is —
 * so the same function draws both and the columns fall off the narrow one.
 *
 * **Only the team the answer carries a roll for opens.** The rest are inert,
 * which is what every other control on this screen is until a Workforce client
 * lands — Rename, Remove and Stand down included. A row that opened an empty
 * pane would be inventing a roll it was never given.
 *
 * @param team the team
 * @param open whether the detail pane is showing it
 * @param grid which of the two widths
 * @param board what the answer carried
 * @param place how to open one
 * @returns the row
 */
function row(
  team: Team, open: boolean, grid: string, board: Teams, place: TeamPlace,
  property: PropertyEnvironment,
): HTMLElement {
  const known = board.detail?.team.id === team.id;
  const classes = `${grid}${open ? " sel" : ""}${team.active ? "" : " down"}`;

  const line = known ? el("button", classes) : el("div", classes);
  if (known) {
    line.setAttribute("type", "button");
    line.addEventListener("click", () => { place.onTeam(team.id); });
  }

  const name = el("div");

  name.append(el("b", undefined, team.name));
  if (team.note !== null && grid === "tgrid") name.append(el("s", undefined, team.note));

  line.append(name, fill(el("div"), dep(team)), el("div", undefined, String(team.members)));

  if (grid === "tgrid") {
    // The formation date carries its year: a team formed in January is
    // read in September, and "4 Jan" of an unstated year is not a date
    // somebody can act on.
    line.append(
      el("div", "tm", formatInstant(team.formed, property, "date-year")),
      status(team));
  }

  return line;
}

/** The canon code, as a chip. */
function dep(team: Team): HTMLElement {
  return el("b", "code neutral", team.department);
}

/** Active, or stood down — ADR 0062's two states, in the property's words. */
function status(team: Team): HTMLElement {
  const cell = el("div");
  cell.append(el("span", `pill ${team.active ? "ok" : "neu"}`,
    team.active ? "Active" : "Stood down"));
  return cell;
}

/** The rule the whole object rests on, under the list. */
function oneDepartment(): HTMLElement {
  const panel = el("div", "note");

  panel.append(el("b", undefined, "A team belongs to one department. "),
    el("span", undefined,
      "Assignment routing is departmental — a job's pool, its policy and its "
      + "accountability are all per department — so a team spanning two would "
      + "make which pool does this sit in unanswerable."));

  return panel;
}

/** Frame 7 — a property that has formed none. */
function firstRun(place: TeamPlace): HTMLElement {
  const empty = el("div", "tvoid");

  const first = el("p");
  first.append(el("b", undefined, "No teams yet."), el("span", undefined,
    " A team is a named group of people in one department — a crew, a floor, a "
    + "shift's regulars — and it exists so work can be given to the group "
    + "rather than to one person."));

  const form = el("button", "btn pri", "＋ Form a team");
  form.setAttribute("type", "button");
  form.addEventListener("click", () => { place.open("form"); });

  empty.append(
    el("div", "big", "⛌"),
    first,
    // The honest second line: a property that never forms one loses nothing.
    el("p", "quiet",
      "Until there is one, jobs are assigned to people. Nothing is waiting on this."),
    form);

  return empty;
}
