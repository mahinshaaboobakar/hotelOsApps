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
 * @param shown how many rows are actually on screen
 * @param go what to do when a page is chosen
 * @returns the pager — always, because the count is information
 */
export function pager(
  total: number,
  page: number,
  size: number,
  shown: number,
  go: (page: number) => void,
): HTMLElement | null {
  const pages = Math.max(1, Math.ceil(total / size));

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
  const first = page * size + 1;

  // Two clamps, and they catch different mistakes. `shown` counts the rows
  // that are actually here, which is what a short page needs — a range wider
  // than the screen is a number nobody can check by counting, and it would read
  // the same if the list had failed to load half of itself. `total` catches a
  // caller that reports a full page on the last page, which is the easy thing
  // to pass and the easy thing to get wrong.
  const last = Math.min(total, first + shown - 1);

  element.append(el(
    "span",
    undefined,
    shown === 0
      ? `no rows on this page · ${total} in the list`
      : `showing ${first}–${last} of ${total}`,
  ));

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
