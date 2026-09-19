/**
 * The day's list — a real table, as the design draws it.
 *
 * # Why a table and not a card per row
 *
 * A receptionist reads this list down a column: every guest's room, then every
 * guest's nights. Cards put each row in its own box and destroy the column, so
 * the comparison the screen exists for has to be made by eye across gaps. The
 * design's hairline dividers and shared grid are the feature.
 *
 * # The empty room is an action, not a state
 *
 * Six of fourteen arrivals having no room is the ordinary case, not a defect:
 * the stay's anchor is the room **type**, and the number is an assignment
 * chosen the night before or at the desk (GUEST-Q2 addendum, S8). So the list
 * is built to be worked in that state — `＋ assign` is inline in the row, and
 * check-in is the one action that refuses to proceed without a room.
 *
 * A party member with no name yet is a real row, drawn italic. Dropping it
 * would hide a booking the desk has to complete.
 */

import { formatNumber, type PropertyEnvironment } from "@hotelos/sdk";

import type { DayRow } from "../../book/model";
import { day } from "../../chrome/when";
import { el, fill, opener, unavailable } from "../../chrome/element";
import { tags } from "../../chrome/marks";

const COLUMNS = ["Guest", "Booking", "Room type", "Room", "Nights", ""] as const;

/**
 * Draw the table.
 *
 * @param rows the day's rows — this page of them
 * @param total how many the LIST holds, which is not how many this page does
 * @param open what to do when a row is chosen
 * @returns the table
 */
export function table(
  rows: readonly DayRow[],
  total: number,
  open: (row: DayRow) => void,
  property: PropertyEnvironment,
): HTMLElement {
  const element = el("div", "tbl");
  const head = el("div", "tr hd");

  for (const column of COLUMNS) {
    head.append(el("div", undefined, column));
  }

  element.append(head);

  // **Only an empty LIST says it is empty.** This tested `rows.length`, so an
  // empty page of a fifty-row list — after a delete while somebody paged —
  // said "Nothing in this list today." over a pager saying "no rows on this
  // page · 50 in the list": two statements on one screen, one of them false.
  // The page-64 audit measured it (G5). An empty page draws no sentence here;
  // the pager states it. The empty list's own words stay as built until 64f.
  if (total === 0) {
    const empty = el("div", "tr");
    empty.append(el("div", "hint", "Nothing in this list today."));
    element.append(empty);
    return element;
  }

  for (const row of rows) {
    element.append(line(row, open, property));
  }

  return element;
}

function line(row: DayRow, open: (row: DayRow) => void, property: PropertyEnvironment): HTMLElement {
  const element = el("div", "tr act");

  const name = el("div", "nm");
  name.append(opener(row.unnamed ? el("b", "un", row.guest) : el("b", undefined, row.guest),
    () => open(row)));

  // The second line, only where there is one to draw. A contact is ruled
  // absent (GUEST-Q12) and a party count is not, so this renders whichever
  // exists and nothing at all when neither does — never an empty span holding
  // the row's height open for a value nobody has.
  const second = row.contact
    ?? (row.party === null ? null : `party of ${formatNumber(row.party, property, "whole")}`);
  if (second !== null) {
    name.append(el("span", undefined, second));
  }

  const room = el("div");
  room.append(
    row.room === null
      // Inline, and a control rather than a chip: the state with the
      // affordance, which is what the list is for.
      ? unavailable("link", "＋ assign", "Assigning a room from GuestOps is not available yet.")
      : document.createTextNode(row.room),
  );

  const chips = el("div");
  fill(chips, ...tags(row.chips));

  element.append(
    name,
    el("div", undefined, row.booking),
    el("div", undefined, row.roomType),
    room,
    el("div", undefined, nights(row, property)),
    chips,
  );

  element.addEventListener("click", () => open(row));
  return element;
}

/**
 * A stay's nights, in the property's form: `03 Sept → 07 Sept`, or the day-use
 * form when the stay arrives and leaves on one day.
 *
 * Composed here, from the two ISO days the service sends — the order and the
 * month's abbreviation are the property's locale's, never the server's. An
 * unrecorded arrival is the dash, never a guessed date.
 */
function nights(row: DayRow, property: PropertyEnvironment): string {
  if (row.arrive === null) return day(null, property, "day-month");

  const arrive = day(row.arrive, property, "day-month");
  if (row.depart === null) return arrive;

  return row.depart === row.arrive
    ? `${arrive} · day use`
    : `${arrive} → ${day(row.depart, property, "day-month")}`;
}
