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

import type { DayRow } from "../../book/model";
import { control, el, fill } from "../../chrome/element";
import { mark } from "../../chrome/marks";

const COLUMNS = ["Guest", "Booking", "Room type", "Room", "Nights", ""] as const;

/**
 * Draw the table.
 *
 * @param rows the day's rows
 * @param open what to do when a row is chosen
 * @returns the table
 */
export function table(rows: readonly DayRow[], open: (row: DayRow) => void): HTMLElement {
  const element = el("div", "tbl");
  const head = el("div", "tr hd");

  for (const column of COLUMNS) {
    head.append(el("div", undefined, column));
  }

  element.append(head);

  if (rows.length === 0) {
    const empty = el("div", "tr");
    empty.append(el("div", "hint", "Nothing in this list today."));
    element.append(empty);
    return element;
  }

  for (const row of rows) {
    element.append(line(row, open));
  }

  return element;
}

function line(row: DayRow, open: (row: DayRow) => void): HTMLElement {
  const element = el("div", "tr act");

  const name = el("div", "nm");
  name.append(
    row.unnamed ? el("b", "un", row.guest) : el("b", undefined, row.guest),
    el("span", undefined, row.contact),
  );

  const room = el("div");
  room.append(
    row.room === null
      // Inline, and a control rather than a chip: the state with the
      // affordance, which is what the list is for.
      ? control("link", "＋ assign")
      : document.createTextNode(row.room),
  );

  const chips = el("div");
  fill(chips, ...row.chips.map(mark));

  element.append(
    name,
    el("div", undefined, row.booking),
    el("div", undefined, row.roomType),
    room,
    el("div", undefined, row.nights),
    chips,
  );

  element.addEventListener("click", () => open(row));
  return element;
}
