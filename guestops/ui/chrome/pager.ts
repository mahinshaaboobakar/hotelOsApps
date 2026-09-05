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

import { control, el } from "./element";

/** How many numbered buttons before the row elides. */
const SHOWN = 5;

/**
 * Draw the pager for a list.
 *
 * @param total how many rows the whole list holds
 * @param page the current page, 0-based
 * @param size how many rows a page holds
 * @param go what to do when a page is chosen
 * @returns the pager, or null when there is nothing to page
 */
export function pager(
  total: number,
  page: number,
  size: number,
  go: (page: number) => void,
): HTMLElement | null {
  const pages = Math.max(1, Math.ceil(total / size));

  // A list that fits on one page gets no pager at all. A single disabled page
  // button is a control that can never do anything, which reads as a broken
  // one rather than as a short list.
  if (pages < 2) {
    return null;
  }

  const element = el("div", "pager");
  const first = page * size + 1;
  const last = Math.min(total, (page + 1) * size);

  element.append(el("span", undefined, `showing ${first}–${last} of ${total}`));

  const nav = el("span", "pnav");
  nav.append(step("‹", page - 1, page > 0, go));

  for (const number of numbers(page, pages)) {
    nav.append(
      number === null
        ? el("span", "gap", "…")
        : step(String(number + 1), number, true, go, number === page),
    );
  }

  nav.append(step("›", page + 1, page < pages - 1, go));
  element.append(nav);

  return element;
}

/** One page button, or an arrow.  */
function step(
  label: string,
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

  // `disabled` rather than a class: an arrow at the end of the list must not
  // take focus or fire, and styling alone would leave it clickable to a
  // keyboard.
  if (!enabled) {
    button.setAttribute("disabled", "");
  }

  return button;
}

/**
 * Which page numbers to draw, with `null` where the row elides.
 *
 * Jobs' four pages are drawn whole and its canonical rendering says nothing
 * about a hundred, because a board never has a hundred. A property's stay list
 * will, and a row of two hundred buttons is not the same design at a larger
 * size — it is a different one. The window keeps the first, the last, and
 * `SHOWN` around the current page.
 */
function numbers(page: number, pages: number): readonly (number | null)[] {
  if (pages <= SHOWN + 2) {
    return [...Array(pages).keys()];
  }

  const half = Math.floor(SHOWN / 2);
  const from = Math.min(Math.max(page - half, 1), pages - SHOWN - 1);
  const window = [...Array(SHOWN).keys()].map((offset) => from + offset);

  return [
    0,
    ...(from > 1 ? [null] : []),
    ...window,
    ...(from + SHOWN < pages - 1 ? [null] : []),
    pages - 1,
  ];
}
