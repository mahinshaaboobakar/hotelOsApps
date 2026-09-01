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
    person(swap.proposer, swap.proposerShifts),
    el("div", "arrow", "⇄"),
    person(swap.colleague, swap.colleagueShifts),
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
  acts.append(el("div", "btn", "Decline…"), el("div", "btn go", "Approve swap"));

  card.append(title, steps, pair, note, atomic, acts);
  return card;
}

/** One side of the exchange, before and after. */
function person(name: string, shifts: readonly [string, string]): HTMLElement {
  const side = el("div", "side");

  side.append(
    el("u", undefined, name),
    el("div", "move", `${shifts[0]} → ${shifts[1]}`),
  );

  return side;
}
