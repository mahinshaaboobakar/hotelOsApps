/**
 * The numbered pager — `CORE-Q13`, and the app surface standard §6.
 *
 * # The arithmetic is the SDK's, and that is the whole point
 *
 * `pagedView` is published by `@hotelos/sdk` so that the desktop's React pager
 * and this DOM one compute the same numbers from the same server answer. The
 * arithmetic is where the mistakes are: a range computed from the page rather
 * than clamped to the total reads *"37–48 of 47"*, and a page count taken from
 * the size a caller **asked** for rather than the size the server **applied**
 * numbers every button wrongly while the list underneath looks perfect. Written
 * twice, those are wrong in two different ways, and the copy nobody is looking
 * at stays wrong.
 *
 * What is left here is markup, which is the part that genuinely differs between
 * a React desktop and a DOM module, and the part where differing costs nothing.
 *
 * # It matches the design system rather than importing it
 *
 * A hosted module is styled by tokens and never by importing components across
 * a realm — so the match is a rendering obligation, not a dependency, and the
 * control names come from `PAGER_LABELS` so two realms cannot name them
 * differently.
 *
 * # It is the list's floor, and it draws on one page
 *
 * §6 as ruled 2026-09-05: the list grows and the strip sticks, so the control
 * is at the bottom whether the list is three rows or a full page. The
 * placement is in `chrome/styles.ts`; what is here is what it says.
 *
 * # One drawing, because Workforce has one pattern
 *
 * Only the paged one is here. The cursor drawings — *Previous · Next* and
 * *Show more* — belong to feeds that rotate under the reader, and this module
 * has none: every read is bounded by a natural key, and the one list long
 * enough to page is the property's postings, whose count is a fact. Writing the
 * other two against no caller would be inventing ahead of need.
 */

import { PAGER_LABELS, pagedView, type Paging } from "@hotelos/sdk";

import { el } from "./element";

/**
 * Draw the pager, or nothing when there is nothing to page.
 *
 * @param paging the server's own numbers — page, the size applied, the total
 * @param shown how many rows the screen actually received
 * @param go called with a 0-based page
 * @returns the row, or null when the list is empty
 */
export function pager(
  paging: Paging, shown: number, go: (page: number) => void,
): HTMLElement | null {
  // **The range counts the rows that ARE there** — §6, and the reason the SDK
  // takes `shown`: `(page + 1) * pageSize` agrees on every full page and lies
  // exactly where it matters. A short page — the last one, a server that
  // clamped, a fixture that samples — was described by the size that had been
  // ASKED for. GuestOps said "showing 1–25 of 218" over nine visible rows.
  const view = pagedView(paging, shown);

  // **It draws on a single page too** — §64 §6, ruled 2026-09-05, and this
  // module had the behaviour that ruling rejects.
  //
  // The old argument here was that a disabled page button can never do
  // anything. True of the BUTTONS and false of the COUNT, and the count is the
  // information: *showing 1–14 of 14* tells a supervisor the list in front of
  // them is the whole list, which they cannot infer from a list that simply
  // stops. The buttons stay, disabled, because a pager that changes shape
  // between one page and two is two controls.
  //
  // An EMPTY property is still nothing: `view.empty` is total zero, where the
  // screen draws its first run and a pager would sit under an empty state
  // promising pages of nothing. §6's empty case is an empty PAGE of a list that
  // has rows elsewhere, which is a different sentence.
  if (view.empty) return null;

  const row = el("div", "pager");

  // A page of a non-empty list that holds nothing says so, rather than
  // describing rows that are not there.
  row.append(el("div", "showing", view.barren
    ? `No rows on this page · ${paging.total} in the list`
    : `Showing ${view.from}–${view.to} of ${paging.total}`));

  const nav = el("div", "pnav");
  nav.append(step("‹", PAGER_LABELS.previousPage, view.hasPrevious,
    () => { go(paging.page - 1); }));

  for (const entry of view.entries) {
    // An elision is a gap, never a button: a reader must not be able to click
    // something whose destination the pager declined to name.
    nav.append(entry === null
      ? el("span", "elide", "…")
      : number(entry, entry === paging.page, go));
  }

  nav.append(step("›", PAGER_LABELS.nextPage, view.hasNext,
    () => { go(paging.page + 1); }));

  row.append(nav);
  return row;
}

/** One page number. Drawn 1-based, carried 0-based. */
function number(page: number, current: boolean, go: (page: number) => void): HTMLElement {
  const button = el("button", current ? "pg on" : "pg", String(page + 1));

  button.setAttribute("type", "button");
  if (current) button.setAttribute("aria-current", "page");
  button.addEventListener("click", () => { go(page); });

  return button;
}

/** An arrow. Present and disabled at the ends, rather than disappearing. */
function step(
  glyph: string, label: string, enabled: boolean, go: () => void,
): HTMLElement {
  const button = el("button", "pg", glyph);

  button.setAttribute("type", "button");
  button.setAttribute("aria-label", label);

  if (enabled) {
    button.addEventListener("click", go);
  } else {
    // Disabled rather than removed: a row whose controls move as you page is a
    // row you have to re-find on every click.
    button.setAttribute("disabled", "");
  }

  return button;
}
