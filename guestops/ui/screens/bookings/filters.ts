/**
 * How a receptionist finds a booking — frame 2's search and its three filters.
 */

import type { Bookings } from "../../book";
import { control, el } from "../../chrome/element";

/**
 * The search box and the three filters, with the screen's two actions.
 *
 * **The search covers what a guest can actually say** — their name, the number
 * they booked with, or the confirmation number the source gave them. That is
 * the placeholder's own list, and it is the reason the list has a search at all
 * rather than a filter on reference: a guest at the counter does not know what
 * the property calls their booking.
 *
 * The four boxes are drawn, not typed into: nothing behind this screen accepts
 * a query yet, and a box that swallowed a receptionist's typing and returned
 * the same nine rows would be worse than one that plainly shows what it filters
 * on. `chrome/field.ts` says the same thing at more length.
 *
 * @param bookings the page, and what the filters are set to
 * @param walkIn what the Walk-in button does
 * @param book what the New booking button does
 * @returns the row
 */
export function filters(
  bookings: Bookings,
  walkIn: () => void,
  book: () => void,
): HTMLElement {
  const row = el("div", "fltr");

  const search = el("div", "inp q");
  search.append(
    document.createTextNode("🔍 "),
    el(
      "span",
      "ph",
      bookings.search === ""
        ? "Name, phone, email, confirmation number…"
        : bookings.search,
    ),
  );

  row.append(search);

  for (const filter of bookings.filters) {
    const showing = filter.choices.find((choice) => choice.on) ?? filter.choices[0];
    const box = el("div", "inp");

    box.append(
      document.createTextNode(showing?.label ?? ""),
      el("span", "grow", "▾"),
    );

    row.append(box);
  }

  row.append(
    control("btn", "Walk-in", walkIn),
    control("btn pri", "＋ New booking", book),
  );

  return row;
}
