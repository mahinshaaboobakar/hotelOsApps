/**
 * The day's list — a real table, as the design draws it.
 *
 * # Why a table and not a card per row
 *
 * A receptionist reads this list down a column: every guest's room, then every
 * guest's nights. Cards put each row in its own box and destroy the column, so
 * the comparison the screen exists for has to be made by eye across gaps. The
 * design's hairline dividers and shared grid are the feature.
 *
 * # The empty room is an action, not a state
 *
 * Six of fourteen arrivals having no room is the ordinary case, not a defect:
 * the stay's anchor is the room **type**, and the number is an assignment
 * chosen the night before or at the desk (GUEST-Q2 addendum, S8). So the list
 * is built to be worked in that state — `＋ assign` is inline in the row, and
 * check-in is the one action that refuses to proceed without a room.
 *
 * A party member with no name yet is a real row, drawn italic. Dropping it
 * would hide a booking the desk has to complete.
 */

import { formatNumber, type PropertyEnvironment } from "@hotelos/sdk";

import type { DayRow } from "../../book/model";
import { span } from "../../chrome/when";
import { control, el, fill, opener } from "../../chrome/element";
import { tags } from "../../chrome/marks";

const COLUMNS = ["Guest", "Booking", "Room type", "Room", "Nights", ""] as const;

/**
 * The three things a row can do — named rather than ordered.
 *
 * **Three callbacks of one type, bound by position, is a swap no compiler can
 * see.** `open`, `noShow` and `assign` all take a row and return nothing, so
 * passing them in the wrong order compiles and misroutes two controls — the
 * positional-argument hazard, which bites exactly when one type fills more
 * than one parameter. Named fields make it inexpressible.
 */
export interface DayActs {
  /** Open the stay. */
  open: (row: DayRow) => void;

  /** Record that nobody came — the owner's N2, ruled 2026-09-24. */
  noShow: (row: DayRow) => void;

  /**
   * Give this stay a room — the day list's `＋ assign`.
   *
   * **The sheet existed and nothing reached it.** `overlays.ts` and
   * `screens/assign/index.ts` each said, in their own words, *"and the day
   * list's `＋ assign`"* — a guarantee asserted in two files and wired in
   * neither, while the control was drawn off saying it was unavailable. The
   * owner's gold page draws it LIVE on seven rows.
   *
   * It routes to the stay and opens the sheet there, exactly as `noShow`
   * does: one sheet, one place, and the stay is what the desk lands on if
   * they close it.
   */
  assign: (row: DayRow) => void;
}

/**
 * Draw the table.
 *
 * @param rows the day's rows — this page of them
 * @param total how many the LIST holds, which is not how many this page does
 * @param acts what a row can do, by name
 * @param property whose zone and conventions the values are drawn in
 * @returns the table
 */
export function table(
  rows: readonly DayRow[],
  total: number,
  acts: DayActs,
  property: PropertyEnvironment,
): HTMLElement {
  const element = el("div", "tbl");
  const head = el("div", "tr hd");

  for (const column of COLUMNS) {
    head.append(el("div", undefined, column));
  }

  element.append(head);

  // **Only an empty LIST says it is empty.** This tested `rows.length`, so an
  // empty page of a fifty-row list — after a delete while somebody paged —
  // said "Nothing in this list today." over a pager saying "no rows on this
  // page · 50 in the list": two statements on one screen, one of them false.
  // The page-64 audit measured it (G5). An empty page draws no sentence here;
  // the pager states it. The empty list's own words stay as built until 64f.
  if (total === 0) {
    const empty = el("div", "tr");
    empty.append(el("div", "hint", "Nothing in this list today."));
    element.append(empty);
    return element;
  }

  for (const row of rows) {
    element.append(line(row, acts, property));
  }

  return element;
}

function line(
  row: DayRow,
  acts: DayActs,
  property: PropertyEnvironment,
): HTMLElement {
  const element = el("div", "tr act");

  const name = el("div", "nm");
  name.append(opener(row.unnamed ? el("b", "un", row.guest) : el("b", undefined, row.guest),
    () => acts.open(row)));

  // The second line, only where there is one to draw. A contact is ruled
  // absent (GUEST-Q12) and a party count is not, so this renders whichever
  // exists and nothing at all when neither does — never an empty span holding
  // the row's height open for a value nobody has.
  const second = row.contact
    ?? (row.party === null ? null : `party of ${formatNumber(row.party, property, "whole")}`);
  if (second !== null) {
    name.append(el("span", undefined, second));
  }

  const room = el("div");
  room.append(
    row.room === null
      // Inline, and a control rather than a chip: the state with the
      // affordance, which is what the list is for.
      //
      // **It drew OFF until 2026-10-04**, saying *"assigning a room from
      // GuestOps is not available yet"* — and the sheet had been there all
      // along, named by `overlays.ts` and by `screens/assign/index.ts`'s own
      // header. The owner's gold page draws it live on seven rows. The old
      // sentence is recorded here because a reader meeting only the wiring
      // cannot tell a route that was always intended from one somebody added.
      ? control("link", "＋ assign", () => acts.assign(row))
      : document.createTextNode(row.room),
  );

  const chips = el("div");
  fill(chips, ...tags(row.chips));

  // **The owner's N2, ruled 2026-09-24.** Closing the day is a list task —
  // two or three rows, one pass — and opening each stay to record it is the
  // same number of decisions and four times the clicks. It sits in the column
  // the build leaves unlabelled, which is where the frame draws it.
  //
  // The build's columns are `Guest · Booking · Room type · Room · Nights · ―`
  // and the frame's are `Guest · Booking · Room · Dates · State · ―`. That
  // difference is older than this action and is not closed here.
  if (row.mayRecordNoShow) {
    chips.append(control("link", "nobody came", () => acts.noShow(row)));
  }

  element.append(
    name,
    el("div", undefined, row.booking),
    el("div", undefined, row.roomType),
    room,
    el("div", undefined, span(row.arrive, row.depart, property)),
    chips,
  );

  // **A click on a control is not a click on the row.** Decided once, here,
  // rather than by every action remembering to stop propagation — the same
  // reasoning ADR 0111 gives for the context menu, and the same failure it
  // avoids: the dock tile and the launcher tile had both already forgotten.
  // Without it, pressing `nobody came` would ALSO open the stay behind the
  // dialog it opens.
  element.addEventListener("click", (event) => {
    if ((event.target as Element).closest("button") !== null) return;
    acts.open(row);
  });

  return element;
}

