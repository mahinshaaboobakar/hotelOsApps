/**
 * The Requests tab — the balances, and what this person has asked for.
 *
 * # The balance sits where the decision is made
 *
 * Not on a separate screen. A manager deciding a request needs the number in
 * front of them, and a balance one click away is a number nobody checks.
 */

import { el } from "../../chrome/element";
import type { Balance, LeaveRow } from "../../roster/leave";

/**
 * The four balance cards.
 *
 * @param balances the property's leave types, with this person's days
 * @returns the row of cards
 */
export function balances(balances_: readonly Balance[]): HTMLElement {
  const row = el("div", "bals");

  for (const balance of balances_) {
    // Negative is a state this card must render, not clamp. WF-Q5: hotels
    // override reality daily, and an approved overdraw is that decision on the
    // screen rather than hidden behind a floor of zero.
    const card = el("div", balance.days < 0 ? "bal over" : "bal");

    card.append(
      el("b", undefined, balance.of === null
        ? String(balance.days)
        : `${balance.days} of ${balance.of}`),
      el("div", undefined, balance.type),
    );

    if (balance.note !== "") {
      card.append(el("s", undefined, balance.note));
    }

    row.append(card);
  }

  return row;
}

/** The request list. */
export function requests(rows: readonly LeaveRow[]): HTMLElement {
  const list = el("div", "rows");
  const columns = "1.6fr 120px 60px 110px";

  const head = el("div", "row hd");
  head.style.gridTemplateColumns = columns;
  head.append(
    el("div", undefined, "Request"),
    el("div", undefined, "Dates"),
    el("div", undefined, "Days"),
    el("div", undefined, "Status"),
  );
  list.append(head);

  for (const row of rows) {
    const item = el("div", "row");
    item.style.gridTemplateColumns = columns;

    const what = el("div");
    what.append(el("b", undefined, row.type));

    if (row.note !== "—") {
      what.append(el("s", undefined, row.note));
    }

    item.append(
      what,
      el("div", undefined, row.dates),
      el("div", undefined, String(row.days)),
      el("div", `pill ${tone(row.state)}`, row.state),
    );

    list.append(item);
  }

  return list;
}

/**
 * How a state reads.
 *
 * `Cancelled` is neutral rather than bad: a request withdrawn before the
 * decision credited the balance back and nothing went wrong.
 */
function tone(state: LeaveRow["state"]): string {
  if (state === "Approved") return "ok";
  if (state === "Requested") return "warn";
  if (state === "Declined") return "bad";
  return "neu";
}
