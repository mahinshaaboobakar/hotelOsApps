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

import { formatDay, formatInstant, formatNumber, type PropertyEnvironment } from "@hotelos/sdk";

import { el, fill, unavailable } from "../../chrome/element";
import type { Member, TeamDetail } from "../../roster/team";
import type { TeamPlace } from ".";

/**
 * Draw the detail pane.
 *
 * @param open the team the list has selected
 * @param place how to open a dialog over the screen
 * @returns the pane
 */
export function detail(
  open: TeamDetail, place: TeamPlace, property: PropertyEnvironment,
): HTMLElement {
  const pane = el("div", "panel tdetail");

  const head = el("div", "thead");
  const name = el("div");
  name.append(
    el("b", undefined, open.team.name),
    el("s", undefined, `${open.team.departmentName} · formed `
      + formatInstant(open.team.formed, property, "date-year")));

  // All three open dialogs that write. Rename said *"inert until a client
  // lands, like every other write on this screen"* and was drawn off; the
  // owner met it dead on 0.3.3, and `posting.assign · rename` was served all
  // along, so the client landed (2026-09-19).
  head.append(name, el("div", "grow"),
    action("Rename", "rename", place), action("Stand down", "down", place));

  pane.append(head, department(open), count(open, property), el("div", "tsec", "Members"),
    members(open.members, property), action("＋ Add a member", "member", place), why());

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
function count(open: TeamDetail, property: PropertyEnvironment): HTMLElement {
  const row = el("div", "tkv");

  return fill(row,
    el("em", undefined,
      `Members on ${formatDay(open.onDate, property, "day-month-year")}`),
    el("b", undefined, formatNumber(open.members.length, property, "whole")));
}

/** The people, each with the day they joined. */
function members(
  people: readonly Member[], property: PropertyEnvironment,
): HTMLElement {
  const list = el("div", "tlist");

  for (const person of people) {
    const row = el("div", "tmem");
    const who = el("div");

    // A name Master Data did not answer for stays absent rather than becoming
    // "Unknown" — the two are different facts and neither is a placeholder this
    // module gets to invent.
    who.append(
      el("b", undefined, person.name ?? "—"),
      // **The word is the screen's; the date is the wire's.** The fixture
      // carried the whole phrase - "since 12 Mar" - so the vocabulary of
      // this row lived in the data, where no locale could reach it and no
      // other screen could reuse it.
      el("s", undefined,
        `since ${formatDay(person.since, property, "day-month-year")}`));

    row.append(el("div", "av", person.initials), who, el("div", "grow"),
      unavailable("btn", "Remove", "Members cannot be removed here yet."));

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
