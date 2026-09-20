/**
 * The Requests tab — the balances, and what this person has asked for.
 *
 * # The balance sits where the decision is made
 *
 * Not on a separate screen. A manager deciding a request needs the number in
 * front of them, and a balance one click away is a number nobody checks.
 */

import { formatNumber, type PropertyEnvironment } from "@hotelos/sdk";

import { days } from "../../chrome/dates";
import { control, el } from "../../chrome/element";
import type { Balance, LeaveRow } from "../../roster/leave";
import { withdrawable } from "./withdraw";

/**
 * The four balance cards.
 *
 * @param balances the property's leave types, with this person's days
 * @returns the row of cards
 */
export function balances(
  balances_: readonly Balance[],
  property: PropertyEnvironment,
): HTMLElement {
  const row = el("div", "bals");

  for (const balance of balances_) {
    // Negative is a state this card must render, not clamp. WF-Q5: hotels
    // override reality daily, and an approved overdraw is that decision on the
    // screen rather than hidden behind a floor of zero.
    const card = el("div", balance.days < 0 ? "bal negative" : "bal");

    card.append(
      // Days can be halves, so the default precision rather than whole.
      el("b", undefined, balance.of === null
        ? formatNumber(balance.days, property)
        : `${formatNumber(balance.days, property)} of ${formatNumber(balance.of, property)}`),
      el("div", undefined, balance.type),
    );

    // Composed here — the verb, the unit and the decimal mark are the
    // reader's, and the service sends only the rate (ADR 0174).
    card.append(el("s", undefined, balance.accruesPerMonth === null
      ? "Granted by HR"
      : `Accrues ${formatNumber(balance.accruesPerMonth, property, "at-most-2")} / month`));

    row.append(card);
  }

  return row;
}

/**
 * The request list.
 *
 * @param rows this person's own requests — ADR 0172, the read is the caller's
 * @param property for the dates and the day counts
 * @param onWithdraw called with the row a person asked to withdraw
 * @returns the list
 */
export function requests(rows: readonly LeaveRow[],
  property: PropertyEnvironment,
  onWithdraw: (row: LeaveRow) => void = () => {},
): HTMLElement {
  const list = el("div", "rows");
  // A fifth column for the control — `64g` §4 B. Putting it in the Status cell
  // would have been two things in one column, and the pill is what that column
  // is read for.
  const columns = "1.6fr 120px 60px 110px 96px";

  const head = el("div", "row hd");
  head.style.gridTemplateColumns = columns;
  head.append(
    el("div", undefined, "Request"),
    el("div", undefined, "Dates"),
    el("div", undefined, "Days"),
    el("div", undefined, "Status"),
    // No label: the column holds one control per row and a heading over it
    // would name the button rather than the column.
    el("div", undefined, ""),
  );
  list.append(head);

  for (const row of rows) {
    const item = el("div", "row");
    item.style.gridTemplateColumns = columns;

    const what = el("div");
    what.append(el("b", undefined, row.type));

    if (row.note !== null) {
      what.append(el("s", undefined, row.note));
    }

    // **Only where the service would accept it.** `withdrawable` reads what
    // `CancelAsync` refuses — a decided or already-withdrawn request — so a
    // declined row carries nothing rather than a button that exists to be
    // refused.
    const act = el("div");

    if (withdrawable(row)) {
      act.append(control("btn sm", "Withdraw", () => { onWithdraw(row); }));
    }

    item.append(
      what,
      el("div", undefined, days(row.dates, property)),
      el("div", undefined, formatNumber(row.days, property)),
      el("div", `pill ${tone(row.state)}`, row.state),
      act,
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
