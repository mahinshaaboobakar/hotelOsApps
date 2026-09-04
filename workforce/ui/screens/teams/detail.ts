/**
 * One team, opened — who is in it on the day the header names.
 *
 * # The date in the header is what "members" means
 *
 * A membership is effective-dated, so this pane reads *who was in this team on
 * that day*. It is not a decoration on a live list: *who was in this team in
 * March* is a question a report asks, and it is answered by this screen with the
 * date moved rather than by a second surface.
 */

import { el, fill } from "../../chrome/element";
import type { Member, TeamDetail } from "../../roster/team";
import type { TeamPlace } from ".";

/**
 * Draw the detail pane.
 *
 * @param open the team the list has selected
 * @param place how to open a dialog over the screen
 * @returns the pane
 */
export function detail(open: TeamDetail, place: TeamPlace): HTMLElement {
  const pane = el("div", "panel tdetail");

  const head = el("div", "thead");
  const name = el("div");
  name.append(
    el("b", undefined, open.team.name),
    el("s", undefined, `${open.team.departmentName} · formed ${open.team.formed}`));

  // Rename is inert until a client lands, like every other write on this
  // screen. Stand down and Add a member open dialogs the module already draws,
  // so those two are real — an inert button beside a working one is only
  // confusing when nothing distinguishes them, and here the dialog does.
  head.append(name, el("div", "grow"),
    el("div", "btn", "Rename"), action("Stand down", "down", place));

  pane.append(head, department(open), count(open), el("div", "tsec", "Members"),
    members(open.members), action("＋ Add a member", "member", place), why());

  return pane;
}

/** The department, and the chip it is known by. */
function department(open: TeamDetail): HTMLElement {
  const row = el("div", "tkv");
  const value = el("div");

  value.append(el("b", "code neutral", open.team.department),
    el("span", undefined, ` ${open.team.departmentName}`));

  return fill(row, el("em", undefined, "Department"), value);
}

/** How many, on the day being asked about. */
function count(open: TeamDetail): HTMLElement {
  const row = el("div", "tkv");

  return fill(row,
    el("em", undefined, `Members on ${open.on}`),
    el("b", undefined, String(open.members.length)));
}

/** The people, each with the day they joined. */
function members(people: readonly Member[]): HTMLElement {
  const list = el("div", "tlist");

  for (const person of people) {
    const row = el("div", "tmem");
    const who = el("div");

    // A name Master Data did not answer for stays absent rather than becoming
    // "Unknown" — the two are different facts and neither is a placeholder this
    // module gets to invent.
    who.append(
      el("b", undefined, person.name ?? "—"),
      el("s", undefined, person.since));

    row.append(el("div", "av", person.initials), who, el("div", "grow"),
      el("div", "btn", "Remove"));

    list.append(row);
  }

  return list;
}

/**
 * A control that opens one of the screen's dialogs.
 *
 * @param label what it reads
 * @param what which dialog
 * @param place how to open it
 * @returns the button
 */
function action(label: string, what: string, place: TeamPlace): HTMLElement {
  const button = el("button", "btn", label);
  button.setAttribute("type", "button");
  button.addEventListener("click", () => { place.open(what); });
  return button;
}

/** What the date in the header does, said once. */
function why(): HTMLElement {
  const note = el("div", "note");

  note.append(
    el("b", undefined, "The date in the header is what “members” means. "),
    el("span", undefined,
      "A membership is effective-dated, so this reads who was in this team on "
      + "that day — the question a report asks about March, answered by the "
      + "same screen."));

  return note;
}
