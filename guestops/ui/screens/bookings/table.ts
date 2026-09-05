/**
 * The bookings list — a booking per row, and its stays as a count.
 */

import type { BookingRow } from "../../book";
import { el, fill } from "../../chrome/element";
import { mark } from "../../chrome/marks";

const COLUMNS = ["Guest", "Booking", "Rooms", "Dates", "Status", ""] as const;

/**
 * Draw the list.
 *
 * **A booking's row shows its stays, not a room** — GUEST-Q2. `BK-4471` says
 * *1 of 3 known* and `BK-4506` says *2*, because a booking is a group and the
 * room is a fact about a stay inside it. The two rooms Opera claimed and has
 * not sent are not rows, not placeholders, and not counted.
 *
 * @param rows the page
 * @param open what to do when a booking is chosen
 * @param selected the booking a dialog is currently about, if any
 * @returns the table
 */
export function table(
  rows: readonly BookingRow[],
  open: (row: BookingRow) => void,
  selected?: string,
): HTMLElement {
  const element = el("div", "tbl");
  const head = el("div", "tr list hd");

  for (const column of COLUMNS) {
    head.append(el("div", undefined, column));
  }

  element.append(head);

  if (rows.length === 0) {
    const empty = el("div", "tr list");
    empty.append(el("div", "hint", "No booking matches this search."));
    element.append(empty);
    return element;
  }

  for (const row of rows) {
    element.append(line(row, open, selected));
  }

  return element;
}

function line(
  row: BookingRow,
  open: (row: BookingRow) => void,
  selected?: string,
): HTMLElement {
  const element = el("div", `tr list act${row.id === selected ? " sel" : ""}`);

  const name = el("div", "nm");
  name.append(row.unnamed ? el("b", "un", row.guest) : el("b", undefined, row.guest));

  // GUEST-Q12: the contact is absent, so the second line is absent with it
  // rather than held open by an empty span.
  if (row.contact !== null) {
    name.append(el("span", undefined, row.contact));
  }

  const booking = el("div");
  booking.append(
    // `created here` is the design's own words for a booking this desk made,
    // drawn as the absence it is rather than as a reference nobody issued.
    row.createdHere
      ? el("span", "un", row.reference)
      : document.createTextNode(row.reference),
  );

  if (row.confirmation !== null) {
    booking.append(el("span", "hint", row.confirmation));
  }

  const status = el("div");
  status.append(el("span", `pill ${row.statusTone}`, row.status));

  const chips = el("div");
  fill(chips, ...row.chips.map(mark));

  element.append(
    name,
    booking,
    el("div", undefined, row.rooms),
    el("div", undefined, row.dates),
    status,
    chips,
  );

  element.addEventListener("click", () => open(row));
  return element;
}
