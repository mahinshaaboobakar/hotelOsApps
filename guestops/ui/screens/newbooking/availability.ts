/**
 * What is free, per room type — frame 14's table.
 */

import type { TypeAvailability } from "../../book";
import { el } from "../../chrome/element";
import { mark } from "../../chrome/marks";

const COLUMNS = [
  "Room type", "Total rooms", "Sold", "Out of order", "Stop-sell", "Free",
] as const;

/**
 * Draw the answer.
 *
 * **Every number here has an owner and none of them is stored as inventory**
 * (GUEST-Q7). `total` is Master Data's and is read, never copied; `sold` is our
 * own stays; `out of order` is EngineeringOps's and arrives as an event;
 * `stop-sell` is ours. `free` is the only one that is not somebody's stored
 * fact — it is what remains.
 *
 * That is also why a lagging projection is safe: if the out-of-order read model
 * is a few seconds behind, the answer is **conservative** and no number
 * anywhere becomes wrong. A stored availability table would need all four
 * inputs writing into it — four ways to drift, and a second owner of the truth
 * about rooms.
 *
 * @param types the room types and their counts
 * @returns the table
 */
export function availability(types: readonly TypeAvailability[]): HTMLElement {
  const element = el("div", "tbl");
  const head = el("div", "tr list hd");

  for (const column of COLUMNS) {
    head.append(el("div", undefined, column));
  }

  element.append(head);

  for (const type of types) {
    element.append(line(type));
  }

  return element;
}

function line(type: TypeAvailability): HTMLElement {
  const element = el("div", "tr list");

  const name = el("div", "nm");
  name.append(el("b", undefined, type.roomType));

  // An amount carries three things or it is not an amount — value, currency,
  // and whether tax is included. A room type with no rate set shows none.
  if (type.rate !== null) {
    name.append(el("span", undefined, type.rate));
  }

  element.append(
    name,
    el("div", undefined, String(type.total)),
    el("div", undefined, String(type.sold)),
    attributed(type.outOfOrder, type.outOfOrderBy, "other"),
    attributed(type.stopSold, type.stopSoldWhy, "disagrees"),
    free(type.free),
  );

  return element;
}

/**
 * A count, and who says so.
 *
 * The attribution is the difference between a number and an answer: four
 * suites showing zero free is only defensible if the screen can say a manager
 * held them for a wedding party. A zero carries no attribution because there is
 * nothing to attribute.
 */
function attributed(
  count: number,
  by: string | null,
  tone: "other" | "disagrees",
): HTMLElement {
  const cell = el("div");
  cell.append(document.createTextNode(String(count)));

  if (by !== null) {
    cell.append(mark({ mark: tone, text: by }));
  }

  return cell;
}

/** What remains — the only number on the row nobody stores. */
function free(count: number): HTMLElement {
  const cell = el("div");
  cell.append(el("b", count > 0 ? "n ok" : "n none", String(count)));
  return cell;
}
