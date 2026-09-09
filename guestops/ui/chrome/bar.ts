/**
 * The application bar — where you are, what is waiting, and who you are.
 *
 * **Across the top, not down the side** — `docs/working/64` §3. An installed
 * application navigates from a 56px bar; the platform's own four keep their
 * left rail, because they are the desktop's own furniture and a guest
 * application drawing its own rail competes with the shell's chrome for the
 * same edge of the same screen.
 *
 * Two of the three things the rail carried survive the move, because each is
 * information rather than decoration:
 *
 * * **a count on every item**, not only on Attention — this is the first thing
 *   a receptionist reads, and `Bookings 218` answers a question without
 *   opening the screen;
 * * **the signed-in person**, pushed right rather than sat at a foot there no
 *   longer is. A desk machine is shared, and every write on these screens is
 *   attributed.
 *
 * The third does not: the inset brand bar on the active item becomes Jobs'
 * 2px underline, because the bar is horizontal and an inset left edge on a
 * horizontal tab marks nothing.
 */

import { control, el } from "./element";

/** One bar entry: what it says, and what it counts. */
export interface BarItem {
  label: string;

  /**
   * What the section holds, where that is a number.
   *
   * Optional, because Setup counts nothing — it is configuration, not a list.
   * A `0` or a `—` there would be a figure nobody produced, in the one place a
   * receptionist glances to see whether anything needs them.
   */
  count?: string;

  /** True for Attention, which counts in the warning colour. */
  attention?: boolean;
}

/**
 * Who is signed in, drawn at the right of the bar.
 *
 * **A module is not told.** `ModuleIdentity` carries `id`, `version` and
 * `capabilities`; `PropertyEnvironment` carries `timezone` and `locale`. Neither
 * carries the signed-in person or the property's name, and a realm has no other
 * input — so this is `null` on a real desk, and the bar says so.
 *
 * Absent is not a default. The platform's own words for this shape, in
 * `protocol.ts`: *"`null` means the platform could not establish this here —
 * never use yours."* A name drawn here is an attribution claim on every write
 * these screens make, so inventing one is the gap rule's own example rather
 * than a placeholder worth improving. Parked as `SHELL-Q52`.
 */
export interface Operator {
  name: string;
  where: string;
}

/**
 * The bar.
 *
 * @param items the entries, in the design's order
 * @param current the selected entry's label
 * @param who the signed-in person, or null when the platform did not say
 * @param go what to do when an entry is chosen
 * @returns the bar
 */
export function bar(
  items: readonly BarItem[],
  current: string,
  who: Operator | null,
  go: (label: string) => void,
): HTMLElement {
  const element = el("div", "head");

  const app = el("div", "app");
  app.append(el("div", "mark", "GO"), document.createTextNode("GuestOps"));
  element.append(app);

  for (const item of items) {
    const button = control(
      item.label === current ? "tab on" : "tab",
      item.label,
      () => go(item.label),
    );

    if (item.count !== undefined) {
      button.append(el("span", item.attention === true ? "n att" : "n", item.count));
    }

    element.append(button);
  }

  // Stated, not blank. A bar with nothing in this slot reads as a bar that
  // forgot to draw it; the sentence says which of the two it is.
  const who_ = who === null
    ? el("div", "who none", "operator not established")
    : el("div", "who", `${who.name} · ${who.where}`);

  if (who === null) {
    who_.title =
      "This application is not told who is signed in: the host contract carries "
      + "the package id, version and capabilities, and no person.";
  }

  element.append(who_);

  return element;
}
