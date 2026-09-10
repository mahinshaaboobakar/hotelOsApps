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

/**
 * Who is signed in, drawn at the bar's right.
 *
 * **Three clauses, not two** — `name · department · property`, owner ruling,
 * 2026-09-04. The property looks redundant on a single-property desk and stops
 * looking redundant the day an organization has two, which the corporate model
 * already allows for. A desk machine is shared and every write on these screens
 * is attributed, so the bar says *who*, *for which department*, *at which
 * hotel*.
 */
export interface Operator {
  name: string | null;

  /** Which department they are working in. */
  department: string | null;

  /** Which hotel. */
  property: string | null;

  /** Their role, which the bar has no room for and the rail used to show. */
  role: string | null;
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
  operator: Operator | null,
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

  const named = who(operator);
  if (named !== null) head.append(named);

  return head;
}

/**
 * The operator line, or nothing at all.
 *
 * **Guarded on a non-empty string, not on `!== null`.** The clauses come from
 * this application's own backend over JSON, where a field can arrive absent, as
 * `null`, or as a string a person never typed anything into. `!== null` admits
 * two of those three, and the bar then draws `undefined · undefined` or a
 * line of separators with nothing between them — which reads as a rendering
 * fault rather than as an absence, and sends whoever sees it looking for a bug
 * in the bar.
 *
 * **An unknown clause is dropped, never filled in.** A person known by name at
 * a hotel Master Data has not named draws `Anjali Menon · Front Office` and
 * stops there. Nothing here substitutes "Unknown", the property's id, or the
 * signed-in user's email: the bar attributes every write on these screens, so a
 * name on it is a claim about who is at the desk.
 *
 * **All three unknown draws no element.** Not an empty one, and not a
 * placeholder — the bar simply does not say who is looking at it, which is
 * the honest answer when nobody could establish it.
 *
 * @param operator who is signed in, as far as the backend could say
 * @returns the line, or null when there is nothing true to put on it
 */
function who(operator: Operator | null): HTMLElement | null {
  if (operator === null) return null;

  const clauses = [operator.name, operator.department, operator.property]
    .map((clause) => (typeof clause === "string" ? clause.trim() : ""))
    .filter((clause) => clause.length > 0);

  if (clauses.length === 0) return null;

  return el("div", "who", clauses.join(" · "));
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
