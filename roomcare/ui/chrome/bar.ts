/**
 * The navigation — the 56 px bar with the sections, a second level of tabs,
 * chips, and the pager (page 64 §3, §6).
 */

import { PAGER_LABELS, pagedView, type HostApi, type Paging } from "@hotelos/sdk";

import type { Operator } from "../model";
import { drawing } from "./failure";
import type { Read } from "./load";
import { control, el } from "./element";
import { whole } from "./number";

/**
 * The top bar: the mark, the app's name, the sections, and who is here
 * (`name · department · property`). Null while the read is out; a failed read
 * says so; a part the service does not have is left out, never filled in.
 */
export function head(sections: readonly string[], current: string, operator: Read<Operator> | null, go: (section: string) => void): HTMLElement {
  const bar = el("header", "head");
  const app = el("div", "app");
  app.append(el("div", "mark", "✓"), document.createTextNode("Room Care"));
  bar.append(app);
  for (const section of sections) {
    bar.append(control(section === current ? "tab on" : "tab", section, () => go(section)));
  }
  if (operator !== null && !operator.ok) bar.append(el("div", "who", `${drawing(operator.failure, "who is signed in").label} · who is signed in`));
  if (operator !== null && operator.ok) {
    const o = operator.value;
    const who = el("div", "who");
    // Master Data allows a staff record with no display name. The bar says so where the name would be, never a
    // placeholder name, and never drops the clause so that two clauses pass for three (page 64 §3; HH, 2026-09-19).
    const name = o.name?.trim() ?? "";
    who.append(name === "" ? el("span", "unnamed", "no name in Master Data") : document.createTextNode(name));
    const rest = [o.department, o.property].filter((part): part is string => part !== null && part.trim() !== "");
    if (rest.length > 0) who.append(document.createTextNode(` · ${rest.join(" · ")}`));
    bar.append(who);
  }
  return bar;
}

/** A second level of tabs, inside a section (Setup's seven). */
export function subnav(tabs: readonly string[], current: string, go: (tab: string) => void): HTMLElement {
  const bar = el("nav", "subnav");
  for (const tab of tabs) bar.append(control(tab === current ? "tab on" : "tab", tab, () => go(tab)));
  return bar;
}

/** A chip — a view switcher or a filter; `.btn`, modified. */
export function chip(label: string, on: boolean, click: () => void): HTMLElement {
  const button = control(on ? "btn chip on" : "btn chip", label, click);
  button.setAttribute("aria-pressed", String(on));
  return button;
}

/**
 * A paged list's scroll container — only the list scrolls (CORE-Q28). A div
 * around the table, never the table itself: a table given the free height
 * stretches its rows to fill it instead of scrolling.
 */
export function scroller(table: HTMLElement): HTMLElement {
  table.classList.remove("list");
  const box = el("div", "list");
  box.append(table);
  return box;
}

/**
 * The numbered pager on `common.v1` paged-with-total. It draws on a single
 * page too, states the rows that are there, and sits at the list's floor
 * (§6, CORE-Q13, CORE-Q28) — the SDK's arithmetic, never a second copy. Its
 * numbers — range, total, page size, page labels — are the property's (§12).
 */
export function pager(host: HostApi, paging: Paging, shown: number, noun: string, go: (page: number) => void): HTMLElement {
  const view = pagedView(paging, shown);
  const line = el("div", "pager");
  const said = view.empty
    ? `no ${noun}`
    : view.barren
      ? `no rows on this page · ${whole(host, paging.total)} in the list`
      : `showing ${whole(host, view.from)}–${whole(host, view.to)} of ${whole(host, paging.total)} · ${whole(host, paging.pageSize)} per page`;
  const buttons = el("span");
  const arrow = (text: string, label: string, to: number, dead: boolean): HTMLElement => {
    const button = control("btn pg", text, () => go(to));
    button.setAttribute("aria-label", label);
    if (dead) button.setAttribute("disabled", "true");
    return button;
  };
  buttons.append(arrow("‹", PAGER_LABELS.previousPage, Math.max(0, paging.page - 1), !view.hasPrevious));
  for (const entry of view.entries.length === 0 ? [0] : view.entries) {
    if (entry === null) {
      buttons.append(el("span", "dim", " … "));
      continue;
    }
    const button = control(entry === paging.page ? "btn pg on" : "btn pg", whole(host, entry + 1), () => go(entry));
    if (view.empty) button.setAttribute("disabled", "true");
    buttons.append(button);
  }
  buttons.append(arrow("›", PAGER_LABELS.nextPage, paging.page + 1, !view.hasNext));
  line.append(el("span", undefined, said), buttons);
  return line;
}
