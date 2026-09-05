/**
 * Bookings — everything the property has ever sold. Gold frame 2.
 *
 * **The list is where a guest at the counter is found**, which is what decides
 * its shape: the search covers what a guest can actually say, and every state a
 * booking can be in stays in the list. A cancelled reservation exists, its
 * penalty may be chargeable, and a no-show is reportable — neither is a
 * deletion (S25, S27, ADR 0062).
 *
 * **Numbered paging** — CORE-Q13, ruled 2026-09-04. A property's bookings are
 * bounded and countable, so `218` is a fact and *showing 1–8 of 218* is
 * something the wire can answer. The cursor is kept for feeds that rotate while
 * they are read, which this is not.
 *
 * There is no page heading: the bar already says Bookings, and a screen that
 * printed it again would spend a row of vertical space saying one word twice
 * (docs/working/64 §3).
 */

import type { HostApi } from "@hotelos/sdk";

import { load, recordedBookings, type BookingRow } from "../../book";
import { fill } from "../../chrome/element";
import { standIn } from "../../chrome/marks";
import { pager } from "../../chrome/pager";
import { filters } from "./filters";
import { table } from "./table";

/** How many bookings a page holds. The server clamps whatever it is sent. */
const PAGE = 25;

/**
 * Render the list.
 *
 * @param host the bridge — the only route out of this realm
 * @param into the element this screen owns
 * @param page which page, 0-based
 * @param turn what to do when another page is chosen
 * @param open what to do when a booking is chosen
 * @param walkIn what the Walk-in action does
 * @param book what the New booking action does
 * @param selected the booking a dialog is currently about, if any
 */
export async function bookings(
  host: HostApi,
  into: HTMLElement,
  page: number,
  turn: (page: number) => void,
  open: (row: BookingRow) => void,
  walkIn: () => void,
  book: () => void,
  selected?: string,
): Promise<void> {
  const loaded = await load(host, "reservation.read", "bookings", recordedBookings, {
    page,
    pageSize: PAGE,
  });

  const list = loaded.value;
  const body = document.createElement("div");
  body.className = "body";

  fill(
    body,
    loaded.live ? null : standIn(loaded.because),
    filters(list, walkIn, book),
    table(list.rows, open, selected),
    pager(list.total, page, PAGE, list.rows.length, turn),
  );

  into.replaceChildren(body);
}
