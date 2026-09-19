/**
 * Shift Board — who is on now, by department, and what changes next.
 *
 * The question a duty manager asks without opening anything: *is the property
 * covered at this moment*. It is consulted rather than watched, so it stacks —
 * missing it for an hour costs nothing, which is `SHELL-Q35`'s own test.
 *
 * # The figure counts the property; the list shows what fits
 *
 * Six departments, four rows. That is the size guarantee working: the popover
 * does not resize to content, so *content that does not fit is cut by the
 * widget, not by the shell*.
 */

import { formatInstant, formatNumber, type HostApi, load, type PropertyEnvironment }
  from "@hotelos/sdk";

import { ROSTER_READ } from "../../chrome/permissions";
import { el, fill } from "../../chrome/element";
import type { Changeover } from "../../roster/widget";

import type { ShiftBoard } from "../../roster/widget";
import { span } from "../../chrome/clock";
import { failureCard, card, figures, rows, section } from "../card";

/**
 * Draw the card.
 *
 * @param host the bridge, and the only route out of this realm
 * @returns the card
 */
export async function shiftBoard(host: HostApi): Promise<HTMLElement> {
  const got = await load<ShiftBoard>(host, ROSTER_READ, "shiftBoard");
  if (!got.ok) {
    return failureCard('Shift Board', got.failure, { the: 'who is on now' },
      { host, opens: "rota", again: () => shiftBoard(host) });
  }

  const board = got.value;

  return card("Shift Board", [
    figures([
      { value: formatNumber(board.onNow, host.property, "whole"), label: "on now", tone: "ink" },
      { value: formatNumber(board.departments, host.property, "whole"), label: "departments",
        tone: "muted" },
    ]),
    section("On now"),
    // Composed here, on `coming-up`'s precedent: a panel that knows its rows
    // are spans renders them, because the service cannot — the separator and
    // the hour cycle are the reader's (ADR 0175).
    rows(board.rows.map((row) => row.from === undefined || row.to === undefined
      ? row
      : { ...row, meta: span({ from: row.from, to: row.to }, host.property) }), host),
    changeover(board.nextChange, host.property),
  ]);
}

/**
 * The next changeover, or nothing at all.
 *
 * Null draws no block — *uncomputable is absent, never approximate*. A day whose
 * last shift has started has no next change, and a dash there would read as a
 * figure the widget failed to fetch rather than as one that does not exist.
 *
 * @param change when the next set comes on, or null
 * @returns the block, or null
 */
function changeover(
  change: Changeover | null, property: PropertyEnvironment,
): HTMLElement | null {
  if (change === null) return null;

  const block = el("div", "wchange");
  const switching = el("div", "wswitch");

  switching.append(
    el("span", "ok", `${formatNumber(change.on, property, "whole")} on`),
    el("span", "muted", `${formatNumber(change.off, property, "whole")} off`),
  );

  // **The property's clock, not the server's.** `at` arrives as an
  // instant; a service that had already written "15:00" would be
  // asserting a timezone nobody established, and a Gulf property would
  // read a Kochi hour.
  return fill(
    block,
    section(`Next change · ${formatInstant(change.at, property, "time")}`),
    switching);
}
