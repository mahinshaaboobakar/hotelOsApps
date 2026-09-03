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

import type { HostApi } from "@hotelos/sdk";

import { ROSTER_READ } from "../../chrome/permissions";
import { el, fill } from "../../chrome/element";
import { load } from "../../roster";
import { recordedShiftBoard } from "../../roster/summaries";
import type { Changeover } from "../../roster/widget";

import { card, figures, rows, section } from "../card";

/**
 * Draw the card.
 *
 * @param host the bridge, and the only route out of this realm
 * @returns the card
 */
export async function shiftBoard(host: HostApi): Promise<HTMLElement> {
  const got = await load(host, ROSTER_READ, "shiftBoard", recordedShiftBoard);
  const board = got.value;

  return card("Shift Board", got.live, [
    figures([
      { value: String(board.onNow), label: "on now", tone: "ink" },
      { value: String(board.departments), label: "departments", tone: "muted" },
    ]),
    section("On now"),
    rows(board.rows, host),
    changeover(board.nextChange),
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
function changeover(change: Changeover | null): HTMLElement | null {
  if (change === null) return null;

  const block = el("div", "wchange");
  const switching = el("div", "wswitch");

  switching.append(
    el("span", "ok", `${String(change.on)} on`),
    el("span", "muted", `${String(change.off)} off`),
  );

  return fill(block, section(`Next change · ${change.at}`), switching);
}
