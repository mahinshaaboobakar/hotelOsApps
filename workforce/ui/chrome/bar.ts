/**
 * The app bar — where the module is, and who is looking at it.
 *
 * # An installed application navigates from the top
 *
 * The platform's own four — Core Administration, Operations Center, My Hotel,
 * Software Center — keep the left rail, because they are the desktop's own
 * furniture. This is a guest in that shell, and a guest drawing its own 240px
 * rail competes with the desktop's chrome for the same edge of the same screen.
 * The app surface standard §3, and this file replaced a rail that did exactly
 * that.
 *
 * # Nine destinations, seven tabs
 *
 * They do not fit a 56px bar, and the standard was written from two
 * applications with four sections each. Grouping is the shape §3 sanctions —
 * *the bar carries sections; a view switcher within a section stays in the
 * body* — so two sections carry two views each and the choice moves down one
 * level. Drawn so it can be ruled, and redlined to FF rather than settled here.
 */

import { el } from "./element";

/** One destination in the bar. */
export interface Section {
  /** What it is called, and what identifies it. */
  label: string;

  /** A count, when there is something to say. */
  count?: string;
}

/** Who is signed in, drawn at the bar's right. */
export interface Operator {
  name: string;
  where: string;
  role: string;
}

/**
 * Build the bar.
 *
 * @param sections the destinations
 * @param current which is lit
 * @param operator who is signed in
 * @param go called with the label when one is chosen
 * @returns the bar element
 */
export function bar(
  sections: readonly Section[],
  current: string,
  operator: Operator,
  go: (label: string) => void,
): HTMLElement {
  const head = el("div", "head");
  const app = el("div", "app");

  app.append(el("div", "mark", "W"), el("div", undefined, "Workforce"));
  head.append(app);

  for (const section of sections) {
    const tab = el("button", section.label === current ? "tab on" : "tab");
    tab.setAttribute("type", "button");
    tab.append(el("span", undefined, section.label));

    if (section.count !== undefined) {
      tab.append(el("span", "n", section.count));
    }

    tab.addEventListener("click", () => go(section.label));
    head.append(tab);
  }

  // The person, and where they sit — one line, because the bar is 56px and the
  // rail's three-line block had a column to spend that this does not.
  head.append(el("div", "who", `${operator.name} · ${operator.where}`));

  return head;
}

/**
 * The body's view switcher — the second of the two levels.
 *
 * @param views the choices within the current section
 * @param current which is showing
 * @param go called with the label when one is chosen
 * @returns the strip, or nothing when the section has a single view
 */
export function switcher(
  views: readonly Section[],
  current: string,
  go: (label: string) => void,
): HTMLElement | null {
  // **Absent rather than empty for a single view.** A strip offering one choice
  // is a control that cannot be operated, and a row of vertical space.
  if (views.length < 2) return null;

  const strip = el("div", "tabs");

  for (const view of views) {
    const tab = el("button", view.label === current ? "tab on" : "tab");
    tab.setAttribute("type", "button");
    tab.append(el("span", undefined, view.label));

    if (view.count !== undefined) {
      tab.append(el("span", "cnt", view.count));
    }

    tab.addEventListener("click", () => go(view.label));
    strip.append(tab);
  }

  return strip;
}
