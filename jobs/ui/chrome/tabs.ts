/**
 * The window's chrome — the head with the app mark, the five top tabs, the
 * search and the operator — and the sub-navigation a screen may carry.
 */

import { control, el, fill } from "./element";
import type { Operator } from "../board/model";

/** A destination on the top bar or a sub-navigation. */
export interface Tab {
  label: string;

  /** A count shown after the label, when the screen has one. */
  count?: string;
}

/** The head: mark, top tabs, search, operator. */
export function head(
  tabs: readonly Tab[],
  current: string,
  operator: Operator | null,
  go: (label: string) => void,
): HTMLElement {
  const bar = el("div", "head");
  const app = el("div", "app");
  app.append(el("div", "mark", "⚒"), document.createTextNode("Jobs"));
  bar.append(app);
  for (const tab of tabs) {
    bar.append(control(tab.label === current ? "tab on" : "tab", tab.label, () => go(tab.label)));
  }
  bar.append(el("div", "search", "Search job number, room, summary…"));
  // Nothing, rather than a name nobody established.
  if (operator !== null) bar.append(el("div", "who", `${operator.name} · ${operator.where}`));
  return bar;
}

/** A sub-navigation under a screen's header — the job view's tabs, the settings tabs. */
export function subnav(tabs: readonly Tab[], current: string, go: (label: string) => void, tail?: Node): HTMLElement {
  const bar = el("div", "subnav");
  for (const tab of tabs) {
    const label = tab.count === undefined ? tab.label : `${tab.label} · ${tab.count}`;
    bar.append(control(tab.label === current ? "tab on" : "tab", label, () => go(tab.label)));
  }
  if (tail !== undefined) fill(bar, el("span", "grow"), tail);
  return bar;
}

/**
 * What a pager says about the rows in front of the reader — standard §6.
 *
 * **One sentence for every list in Jobs**, because three lists wrote their own:
 * the Board said *"no jobs in this list"* when empty, while Scheduled computed
 * `1–0 of 0` — arithmetic nobody reads as "this list is empty", which §6
 * names. The range counts the rows that ARRIVED (§6: `first + rows.length - 1`,
 * clamped by `total`), never the page size asked for.
 *
 * @param first the 1-based position of the first row on this page
 * @param rows how many rows this page actually holds
 * @param total how many the list holds in all
 */
export function counted(first: number, rows: number, total: number): string {
  if (rows === 0) {
    return total === 0 ? "no jobs in this list" : `no rows on this page · ${String(total)} in the list`;
  }
  return `${String(first)}–${String(Math.min(total, first + rows - 1))} of ${String(total)}`;
}

/** A pager line — "1–12 of 47" and the page buttons. */
export function pager(shown: string, page: number, pages: number, go: (page: number) => void): HTMLElement {
  const line = el("div", "pager");
  const buttons = el("span");

  // An arrow with nowhere to go is disabled rather than absent: a pager that
  // changes shape between one page and two is two controls, and the count is
  // the information a one-page list carries — it says the list in front of you
  // is the whole list, which a list that simply stops cannot (standard §6).
  const arrow = (text: string, to: number, dead: boolean): HTMLElement => {
    const button = control("btn pg", text, () => go(to));
    if (dead) button.setAttribute("disabled", "true");
    return button;
  };

  buttons.append(arrow("‹", Math.max(0, page - 1), page === 0));
  for (let i = 0; i < pages; i += 1) {
    buttons.append(control(i === page ? "btn pg on" : "btn pg", String(i + 1), () => go(i)));
  }

  buttons.append(arrow("›", Math.min(pages - 1, page + 1), page >= pages - 1));
  line.append(el("span", undefined, shown), buttons);
  return line;
}
