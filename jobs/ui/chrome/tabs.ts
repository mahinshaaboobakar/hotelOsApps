/**
 * The window's chrome — the head with the app mark, the five top tabs, the
 * search and the operator — and the sub-navigation a screen may carry.
 */

import { PAGER_LABELS, formatNumber, pagedView, type Paging, type PropertyEnvironment } from "@hotelos/sdk";

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
  // **No search box** — standard §3: "No search box in the bar unless the app
  // has one" (checklist N4). This drew "Search job number, room, summary…" as a
  // <div> with nothing behind it: Jobs has no search, so the box promised one
  // to every person who looked at the bar. The locked frame draws it; a control
  // that does nothing is not an appearance a frame can approve, and it returns
  // with a search that answers. The spacer keeps the operator at the right.
  bar.append(el("span", "grow"));
  if (operator !== null) bar.append(who(operator));
  return bar;
}

/**
 * Page 64 §3's identity clause: `name · department · property` (checklist N5).
 *
 * Room Care's reading — the parts the service established, joined — with one
 * clause Room Care does not have: Jobs spans departments and has no posting to
 * read until ADR 0203, so the department is said to be **not established**, in
 * words, rather than dropped (which would read as two clauses by design) or
 * filled with a stand-in (which would read as a fact). A missing name or
 * property is dropped, as Room Care drops it.
 */
function who(operator: Operator): HTMLElement {
  const parts: (string | HTMLElement)[] = [];
  if (operator.name) parts.push(operator.name);
  parts.push(operator.department ? operator.department : el("span", "unset", "department not established"));
  if (operator.property) parts.push(operator.property);

  const clause = el("div", "who");
  parts.forEach((part, i) => clause.append(...(i === 0 ? [part] : [" · ", part])));
  return clause;
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
 * A pager line — "1–12 of 47" and the page buttons — drawn from the SDK's ONE
 * pager (standard §6, `CORE-Q13`; checklist G2).
 *
 * **The arithmetic and the arrow names are `@hotelos/sdk`'s**: `pagedView`
 * computes the range from the rows that ARRIVED (clamped by `total`), the page
 * count, the page numbers with their elision, and keeps an empty list apart
 * from an empty page; `PAGER_LABELS` names the arrows. Jobs hand-wrote all of
 * it until 2026-09-19 — including a `counted()` written that same morning to
 * unify three lists' wording, which made it a third copy of what the SDK
 * already owned. The sentences are the caller's, as the SDK says.
 *
 * An arrow with nowhere to go is disabled rather than absent: a pager that
 * changes shape between one page and two is two controls, and the count is the
 * information a one-page list carries (§6).
 *
 * **The empty list (`E0`) is not ruled** — checklist G11, going to the owner as
 * 64f. `pagedView` says *"a caller draws its own empty state"*; Jobs draws the
 * pager with *no jobs in this list* and both arrows disabled, recorded as what
 * is built, not as settled.
 *
 * @param paging page (0-based), the page size applied, and the list's total
 * @param shown how many rows this page actually holds
 * @param property the locale every number is written in — standard §12 (U1)
 * @param suffix what a caller adds after the range, never after an empty state
 */
export function pager(
  paging: Paging,
  shown: number,
  go: (page: number) => void,
  property: PropertyEnvironment,
  suffix?: string,
): HTMLElement {
  const view = pagedView(paging, shown);
  const n = (value: number): string => formatNumber(value, property);
  const line = el("div", "pager");
  const buttons = el("span");

  const sentence = view.empty
    ? "no jobs in this list"
    : view.barren
      ? `no rows on this page · ${n(paging.total)} in the list`
      : `${n(view.from)}–${n(view.to)} of ${n(paging.total)}${suffix === undefined ? "" : ` · ${suffix}`}`;

  const arrow = (glyph: string, name: string, to: number, live: boolean): HTMLElement => {
    const button = control("btn pg", glyph, () => go(to));
    button.setAttribute("aria-label", name);
    if (!live) button.setAttribute("disabled", "true");
    return button;
  };

  buttons.append(arrow("‹", PAGER_LABELS.previousPage, paging.page - 1, view.hasPrevious));
  for (const entry of view.entries) {
    buttons.append(entry === null
      ? el("span", "pg-gap", "…")
      : control(entry === paging.page ? "btn pg on" : "btn pg", n(entry + 1), () => go(entry)));
  }
  buttons.append(arrow("›", PAGER_LABELS.nextPage, paging.page + 1, view.hasNext));

  line.append(el("span", undefined, sentence), buttons);
  return line;
}
