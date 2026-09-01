/**
 * The application rail — where you are, what is waiting, and who you are.
 *
 * Three things the design puts here that a plain nav does not have, and each
 * carries information rather than decoration:
 *
 * * **a count on every item**, not only on Attention — the rail is the first
 *   thing a receptionist reads, and `Bookings 218` answers a question without
 *   opening the screen;
 * * **a brand-coloured bar on the active item**, inset rather than a border, so
 *   the selection survives the item's own hover fill;
 * * **the signed-in person at the foot** — name, role and property. A desk
 *   machine is shared, and every write on these screens is attributed.
 */

import { control, el } from "./element";

/** One rail entry: what it says, and what it counts. */
export interface RailItem {
  label: string;
  count: string;

  /** True for Attention, which counts in the warning colour. */
  attention?: boolean;
}

/** Who is signed in, drawn at the rail's foot. */
export interface Operator {
  name: string;
  where: string;
}

/**
 * The rail.
 *
 * @param items the entries, in the design's order
 * @param current the selected entry's label
 * @param who the signed-in person
 * @param go what to do when an entry is chosen
 * @returns the rail
 */
export function rail(
  items: readonly RailItem[],
  current: string,
  who: Operator,
  go: (label: string) => void,
): HTMLElement {
  const element = el("div", "rail");

  const app = el("div", "app");
  app.append(el("div", "mark", "GO"), document.createTextNode("GuestOps"));
  element.append(app);

  for (const item of items) {
    const button = control(
      item.label === current ? "ri on" : "ri",
      item.label,
      () => go(item.label),
    );

    button.append(el("span", item.attention === true ? "cnt att" : "cnt", item.count));
    element.append(button);
  }

  const me = el("div", "me");
  me.append(el("b", undefined, who.name), document.createTextNode(who.where));
  element.append(me);

  return element;
}
