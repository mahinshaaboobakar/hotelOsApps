/**
 * The stays inside one booking — frames 8 and 9.
 */

import type { BookingStay } from "../../book";
import { el, fill } from "../../chrome/element";
import { mark } from "../../chrome/marks";

const COLUMNS = ["Guest", "Stay", "Room type", "Dates", "Status", ""] as const;

/**
 * Draw the stays.
 *
 * **Every stay is a row and nothing else is.** A booking whose source claimed
 * three rooms and has sent one shows one row — the other two are not rows, not
 * placeholders, and not counted (GUEST-Q2, frame 9). What the source claimed is
 * said in words above the table, where it is a statement about the booking
 * rather than two stays nobody made.
 *
 * @param stays the stays this booking holds
 * @param selected which stay a dialog is about, if any
 * @returns the table
 */
export function table(
  stays: readonly BookingStay[],
  selected?: string,
): HTMLElement {
  const element = el("div", "tbl");
  const head = el("div", "tr list hd");

  for (const column of COLUMNS) {
    head.append(el("div", undefined, column));
  }

  element.append(head);

  for (const stay of stays) {
    element.append(line(stay, selected));
  }

  return element;
}

function line(stay: BookingStay, selected?: string): HTMLElement {
  const element = el("div", `tr list${stay.id === selected ? " sel" : ""}`);

  const name = el("div", "nm");
  name.append(stay.unnamed ? el("b", "un", stay.guest) : el("b", undefined, stay.guest));

  const status = el("div");
  status.append(el("span", `pill ${stay.statusTone}`, stay.status));

  const chips = el("div");
  fill(chips, ...stay.chips.map(mark));

  element.append(
    name,
    el("div", undefined, stay.stayId),
    el("div", undefined, stay.roomType),
    el("div", undefined, stay.dates),
    status,
    chips,
  );

  return element;
}
