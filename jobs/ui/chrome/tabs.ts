/**
 * The window's chrome — the head with the app mark, the five top tabs, the
 * search and the operator — and the sub-navigation a screen may carry.
 */

import { control, el, fill } from "./element";

/** A destination on the top bar or a sub-navigation. */
export interface Tab {
  label: string;

  /** A count shown after the label, when the screen has one. */
  count?: string;
}

/** Who is signed in, drawn at the head's end. */
export interface Operator {
  name: string;
  where: string;
}

/** The head: mark, top tabs, search, operator. */
export function head(
  tabs: readonly Tab[],
  current: string,
  operator: Operator,
  go: (label: string) => void,
): HTMLElement {
  const bar = el("div", "head");
  const app = el("div", "app");
  app.append(el("div", "mark", "⚒"), document.createTextNode("Jobs"));
  bar.append(app);
  for (const tab of tabs) {
    bar.append(control(tab.label === current ? "tab on" : "tab", tab.label, () => go(tab.label)));
  }
  bar.append(
    el("div", "search", "Search job number, room, summary…"),
    el("div", "who", `${operator.name} · ${operator.where}`),
  );
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

/** A pager line — "1–12 of 47" and the page buttons. */
export function pager(shown: string, page: number, pages: number, go: (page: number) => void): HTMLElement {
  const line = el("div", "pager");
  const buttons = el("span");
  buttons.append(control("pg", "‹", () => go(Math.max(0, page - 1))));
  for (let i = 0; i < pages; i += 1) {
    buttons.append(control(i === page ? "pg on" : "pg", String(i + 1), () => go(i)));
  }
  buttons.append(control("pg", "›", () => go(Math.min(pages - 1, page + 1))));
  line.append(el("span", undefined, shown), buttons);
  return line;
}
