/**
 * The Approvals tab — one queue, two kinds.
 *
 * # Why leave and swaps share it
 *
 * `WF-Q9` made a staff swap an object with a lifecycle, so it needs a decision
 * surface — and it belongs beside leave because **both resolve to the same
 * person**: the reporting manager when the posting names one, the department
 * head otherwise. One rule, one queue.
 */

import { formatDay, type PropertyEnvironment } from "@hotelos/sdk";

import { codeChip } from "../../chrome/code";
import { days } from "../../chrome/dates";
import { el, unavailable } from "../../chrome/element";
import type { SwapDetail, Waiting } from "../../roster/leave";

/** The queue. */
export function queue(items: readonly Waiting[],
  property: PropertyEnvironment,
): HTMLElement {
  const list = el("div", "rows");
  const columns = "1.9fr 90px 110px";

  const head = el("div", "row hd");
  head.style.gridTemplateColumns = columns;
  head.append(
    el("div", undefined, "Waiting on you"),
    el("div", undefined, "Kind"),
    el("div", undefined, "Dates"),
  );
  list.append(head);

  for (const item of items) {
    const row = el("div", "row");
    row.style.gridTemplateColumns = columns;

    const what = el("div");
    what.append(el("b", undefined, item.who), el("s", undefined, item.what));

    // A leave row carries a span and a swap row carries the day it was
    // accepted. They shared one field while both were rendered strings,
    // so nothing on the row said which kind it was.
    const when = item.dates !== undefined
      ? days(item.dates, property)
      : item.accepted !== undefined && item.accepted !== null
        ? formatDay(item.accepted, property, "day-month-year")
        : "";

    row.append(what, el("div", "pill neu", item.kind), el("div", undefined, when));
    list.append(row);
  }

  return list;
}

/**
 * The open swap — its three steps, both cells, and who agreed when.
 *
 * **The accept step is visible and already done.** A manager's approval must
 * never commit somebody who did not agree, so the strip shows exactly where the
 * proposal stands rather than presenting it as a fresh decision.
 */
export function swapCard(swap: SwapDetail, property: PropertyEnvironment): HTMLElement {
  const card = el("div", "swap");

  const title = el("div");
  title.append(
    el("div", "ht", `Shift swap · ${formatDay(swap.on, property, "day-month-year")}`),
    el("div", "hsub", `Proposed by ${swap.proposer} · accepted by ${swap.colleague}`),
  );

  const steps = el("div", "steps");
  steps.append(
    el("em", undefined, "Proposed"),
    el("span", undefined, "→"),
    el("em", undefined, "Accepted"),
    el("span", undefined, "→"),
    el("em", "now", "Your approval"),
  );

  const pair = el("div", "pair");
  pair.append(
    person(swap.proposer, swap.proposerWhere, swap.proposerShifts),
    el("div", "arrow", "⇄"),
    person(swap.colleague, swap.colleagueWhere, swap.colleagueShifts),
  );

  // Provenance on the card, never in an audit screen — WF-Q9(b). Who proposed
  // it, from where, and when the colleague agreed.
  const note = el("div", "note", swap.provenance);

  const atomic = el("div", "note");
  atomic.append(
    el("b", undefined, "Approving writes both rota cells together. "),
    el("span", undefined, "Declining leaves the rota untouched and tells both people."),
  );

  const acts = el("div", "acts");
  acts.append(
    unavailable("btn", "Decline…", "Swaps cannot be decided here yet."),
    unavailable("btn pri", "Approve swap", "Swaps cannot be decided here yet."));

  // **No "day after" grid.** There was one — the day's four shift columns, the
  // two people placed into them, and an "Also on duty" row — and every name in
  // it but the two was a literal ("Vishnu", "Priya", "Joseph"), under column
  // names that were the drawing's rather than the property's catalogue. Nothing
  // on the wire says who else is on that day, so the row could only ever be
  // invented (owner ruling, 2026-09-09: no fabricated rows). The two cells the
  // swap changes are the chips above, before and after.
  card.append(title, steps, pair, note, atomic, acts);
  return card;
}

/** Where the open swap would be, when none is — which is always, today. */
export function noSwap(): HTMLElement {
  return el("div", "note", "No swap is open.");
}

/** One side of the exchange, before and after. */
function person(
  name: string,
  where: string,
  shifts: readonly [string, string],
): HTMLElement {
  const side = el("div", "side");
  const move = el("div", "move");

  // Chips, because the approver is about to compare these against the rota, and
  // a code drawn differently in two places is a code somebody has to check.
  move.append(
    codeChip(shifts[0], tone(shifts[0])),
    el("span", undefined, "→"),
    codeChip(shifts[1], tone(shifts[1])),
  );

  // The posting, not just the person: a swap exchanges two POSTINGS, and the
  // zone is what says whether the exchange covers the same ground.
  side.append(el("u", undefined, name), el("s", undefined, where), move);
  return side;
}

/** The catalogue's tone for a code this card shows. */
function tone(code: string): string {
  if (code === "M") return "brand";
  if (code === "A") return "ok";
  if (code === "N") return "warn";
  return "neutral";
}
