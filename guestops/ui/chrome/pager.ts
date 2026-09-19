/**
 * The pager — numbered, because the wire carries a total.
 *
 * `ListStays` pages on `PagedRequest`/`PagedResponse` (`CORE-Q13`), so both an
 * ordinal and a count exist and *"showing 1–25 of 47"* is something the service
 * can actually answer. The Previous/Next drawing this replaces was correct for
 * the cursor and was superseded by the ruling, not by a redesign.
 *
 * # It matches `components/design/pager.tsx`; it does not import it
 *
 * That component is React, in the Shell's bundle. This module is plain DOM in a
 * realm with `default-src 'none'`, and the design system reaches a hosted module
 * as **tokens, not components** — `SHELL-Q30`, bound 1. So the match is a
 * rendering obligation rather than a dependency, and the numbers below are
 * Jobs' board as that component draws it: the range on the left, the pages on
 * the right, the current one carrying the brand border, eliding past five.
 */

import {
  formatNumber, PAGER_LABELS, pagedView, type PropertyEnvironment,
} from "@hotelos/sdk";

import { control, el } from "./element";

/**
 * Draw the pager for a list.
 *
 * # The arithmetic and the arrows' names are the SDK's — §6, G2
 *
 * *"ONE pager component — the words are `PAGER_LABELS`, the arithmetic
 * `pagedView`."* This computed its own range, its own page window (eliding past
 * five) and its own labels until the page-64 audit, 2026-09-19 — a second copy
 * of what four applications must agree on. The window now elides where the SDK
 * does (`WHOLE_ROW`), which moves the row on a long list; the standard governs.
 *
 * # Every number goes through `formatNumber` — §12, U1
 *
 * In the property's locale, and ungrouped where none is established — the
 * SDK's rule, not a separator chosen here.
 *
 * @param total how many rows the whole list holds
 * @param page the current page, 0-based
 * @param size how many rows a page holds
 * @param shown how many rows are actually on screen
 * @param go what to do when a page is chosen
 * @param property the property's locale, for the numbers
 * @returns the pager — always, because the count is information
 */
export function pager(
  total: number,
  page: number,
  size: number,
  shown: number,
  go: (page: number) => void,
  property: PropertyEnvironment,
): HTMLElement | null {
  const view = pagedView({ page, pageSize: size, total }, shown);
  const n = (value: number): string => formatNumber(value, property, "whole");

  // **The range is drawn even on a single page**, and the nav with it.
  //
  // This used to return null for a one-page list, on the argument that a
  // disabled page button is a control that can never do anything. That was an
  // implementation opinion and it lost: gold frame 1 draws `showing 1–14 of 14`
  // over a fourteen-row list with `‹ 1 ›` beneath it, and the owner rejected
  // the build for the pager's absence (2026-09-05). The count is the
  // information — it tells a receptionist the list in front of them is the
  // whole list, which is exactly what a person checking the morning's arrivals
  // needs to know and cannot infer from a list that simply stops.
  const element = el("div", "pager");

  // `from`/`to` are `pagedView`'s: counted from the rows that arrived, clamped
  // by the total — both clamps this file used to keep for itself.
  //
  // **The empty list (E0) is drawn as built, deliberately.** `pagedView` says
  // *"a caller draws its own empty state"*, and what E0 draws is OPEN (G11,
  // going to the owner as 64f). Until that is ruled, E0 keeps the sentence and
  // the lone page it had — changing it here would decide 64f by refactor.
  element.append(el(
    "span",
    undefined,
    view.empty || view.barren
      ? `no rows on this page · ${n(total)} in the list`
      : `showing ${n(view.from)}–${n(view.to)} of ${n(total)}`,
  ));

  // **The rows-per-page statement, drawn here so no screen can omit it** —
  // `64` §5. Jobs says *"12 per page at this height; a maximised window shows
  // 24"* on the page; GuestOps said nothing anywhere, on any screen. Putting it
  // in the pager rather than in each screen's body is the same reasoning as the
  // range beside it: an author who has a pager has this, and an author who
  // forgets cannot forget it separately.
  //
  // It states the size and not a guess at what a taller window would hold —
  // this module does not resize its page, so a sentence about a maximised
  // window would be a claim about behaviour it does not have.
  element.append(el("span", "psize", `${n(size)} per page`));

  const nav = el("span", "pnav");
  nav.append(step("‹", PAGER_LABELS.previousPage, page - 1, view.hasPrevious, go));

  // E0 keeps its lone page 1, as built (above); every other state draws the
  // SDK's entries.
  for (const number of view.empty ? [0] : view.entries) {
    nav.append(
      number === null
        ? el("span", "gap", "…")
        : step(n(number + 1), null, number, true, go, number === page),
    );
  }

  nav.append(step("›", PAGER_LABELS.nextPage, page + 1, view.hasNext, go));
  element.append(nav);

  return element;
}

/** One page button, or an arrow — an arrow named by `PAGER_LABELS`.  */
function step(
  label: string,
  name: string | null,
  target: number,
  enabled: boolean,
  go: (page: number) => void,
  current = false,
): HTMLElement {
  const button = control(current ? "pg on" : "pg", label, () => {
    if (enabled) {
      go(target);
    }
  });

  // A glyph is not a name; the SDK names it so two realms cannot differ.
  if (name !== null) button.setAttribute("aria-label", name);
  if (current) button.setAttribute("aria-current", "page");

  // `disabled` rather than a class: an arrow at the end of the list must not
  // take focus or fire, and styling alone would leave it clickable to a
  // keyboard.
  if (!enabled) {
    button.setAttribute("disabled", "");
  }

  return button;
}

