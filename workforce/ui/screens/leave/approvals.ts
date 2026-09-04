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

import { codeChip } from "../../chrome/code";
import { el } from "../../chrome/element";
import type { SwapDetail, Waiting } from "../../roster/leave";

/** The queue. */
export function queue(items: readonly Waiting[]): HTMLElement {
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

    row.append(what, el("div", "pill neu", item.kind), el("div", undefined, item.dates));
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
export function swapCard(swap: SwapDetail): HTMLElement {
  const card = el("div", "swap");

  const title = el("div");
  title.append(
    el("div", "ht", `Shift swap · ${swap.when}`),
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
  acts.append(el("div", "btn", "Decline…"), el("div", "btn pri", "Approve swap"));

  card.append(title, steps, pair, preview(swap), note, atomic, acts);
  return card;
}

/**
 * The day after the swap, before anybody agrees to it.
 *
 * **The two-cell atomic exchange, shown rather than described.** Approval writes
 * both cells together, so what an approver needs is the shift the day ends up
 * in — not a sentence promising it. A decision surface that described its own
 * effect and did not draw it is the one somebody approves twice to see what
 * happened.
 */
function preview(swap: SwapDetail): HTMLElement {
  const box = el("div", "after");

  const grid = el("div", "agrid");
  for (const heading of [swap.when.split(" ").slice(-2).join(" "),
    "Morning", "Afternoon", "Night", "Cover"]) {
    grid.append(el("div", "rhd", heading));
  }

  // After: the proposer takes what the colleague held, and the reverse.
  grid.append(el("div", "alab", "After the swap"));
  grid.append(
    el("div", "acell", swap.proposerShifts[1] === "M" ? swap.proposer.split(" ")[0] ?? "" : ""),
    el("div", "acell", swap.colleagueShifts[1] === "A" ? swap.colleague.split(" ")[0] ?? "" : ""),
    el("div", "acell", "Vishnu"),
    el("div", "acell dim", "—"),
  );

  grid.append(el("div", "alab", "Also on duty"));
  grid.append(
    el("div", "acell", "Priya"), el("div", "acell", "Joseph"),
    el("div", "acell dim", "—"), el("div", "acell dim", "—"),
  );

  box.append(grid);
  return box;
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
