/**
 * The navigation rail — where the module is, and what is waiting.
 *
 * The eight destinations the approved frames draw, in their order. Counts sit
 * at the right and **come from the lists themselves**: a rail that carried its
 * own number would eventually disagree with the screen it points at, and the
 * rail is the one a person believes.
 */

import { el } from "./element";

/** One destination. */
export interface RailItem {
  /** What it is called, and what identifies it. */
  label: string;

  /** The glyph the frames draw beside it. */
  glyph: string;

  /** A count, when there is something to say. */
  count?: string;
}

/** Who is signed in, drawn at the rail's foot. */
export interface Operator {
  name: string;
  where: string;
  role: string;
}

/**
 * Build the rail.
 *
 * @param items the destinations
 * @param current which is lit
 * @param operator who is signed in
 * @param go called with the label when one is chosen
 * @returns the rail element
 */
export function rail(
  items: readonly RailItem[],
  current: string,
  operator: Operator,
  go: (label: string) => void,
): HTMLElement {
  const bar = el("div", "rail");
  const app = el("div", "app");

  app.append(el("div", "mark", "W"), el("div", undefined, "Workforce"));
  bar.append(app);

  for (const item of items) {
    const row = el("div", item.label === current ? "ri on" : "ri");
    row.append(el("span", undefined, item.glyph), el("span", undefined, item.label));

    if (item.count !== undefined) {
      row.append(el("span", "cnt", item.count));
    }

    row.addEventListener("click", () => go(item.label));
    bar.append(row);
  }

  const me = el("div", "me");
  me.append(
    el("b", undefined, operator.name),
    el("div", undefined, operator.where),
    el("div", undefined, operator.role),
  );

  bar.append(me);
  return bar;
}
