/**
 * Withdrawing your own leave request.
 *
 * # It is the person's own row, and only their own
 *
 * The approver's panel decides somebody else's request; this is the other side
 * of `64g` §4 B — *withdraw on the person's own row*. The list it sits in is
 * `board.requests`, which is the caller's own leave (ADR 0172), so there is no
 * field naming whose request it is and no way to express another person's.
 *
 * # What it says depends on what the service will do
 *
 * An approved request has already been debited, and `LeaveService.CancelAsync`
 * credits it back; a request still waiting was never debited at all. Those are
 * two different sentences, and a single one covering both would be wrong for
 * one of them — so the note is written from the state the row carries.
 */

import { formatNumber, type HostApi, type PropertyEnvironment } from "@hotelos/sdk";

import { foot } from "../../chrome/confirm";
import { days } from "../../chrome/dates";
import { el } from "../../chrome/element";
import { overlay } from "../../chrome/overlay";
import { UNKNOWN_OUTCOME, write, WriteRefused } from "../../roster";
import type { LeaveRow } from "../../roster/leave";

/**
 * Whether a request can be withdrawn at all.
 *
 * Read from what the service accepts rather than from what reads well:
 * `CancelAsync` refuses a request that is already `Declined` or `Cancelled`, so
 * those two rows carry no control. A button that exists to be refused is a
 * control that looks live and does nothing.
 */
export function withdrawable(row: LeaveRow): boolean {
  return row.state === "Requested" || row.state === "Approved";
}

/**
 * Build the dialog.
 *
 * @param host the bridge
 * @param row the request being withdrawn
 * @param property for the dates and the number of days
 * @param close called when it is dismissed
 * @param done called after it is withdrawn, so the board is re-read
 * @returns the overlay
 */
export function withdrawRequest(
  host: HostApi,
  row: LeaveRow,
  property: PropertyEnvironment,
  close: () => void,
  done: () => void,
): HTMLElement {
  const head = el("div");
  head.append(
    el("div", "ht", `Withdraw your ${row.type} request?`),
    // Composed here, from the two ends and the count the wire carries — the
    // service sends neither the separator nor the word for *days*.
    el("div", "hsub",
      `${days(row.dates, property)} · ${formatNumber(row.days, property)} days`));

  const refusal = el("div", "note warn");

  // Destructive, so the confirm is filled — page 64 §2. Nothing is outstanding:
  // the row is the one the person pressed, so this is live from the moment it
  // opens rather than `off` with a reason.
  const acts = foot("Withdraw", "Withdrawing…", close, "destructive");

  acts.onConfirm(() => {
    void (async () => {
      refusal.replaceChildren();
      acts.working(true);

      try {
        // The id and the version the read carried. A withdraw that sent no
        // version would overwrite a decision made while this dialog was open.
        await write(host, "leave.request", "withdraw", {
          id: row.id,
          version: row.version,
        });
        done();
      } catch (error) {
        // The overlay stays open carrying the reason (§9): a dialog that closed
        // on a failed write would leave somebody believing their leave was
        // withdrawn while the rota still has them away.
        refusal.append(el("span", undefined,
          error instanceof WriteRefused
            ? error.message
            : UNKNOWN_OUTCOME));
        acts.working(false);

        if (!(error instanceof WriteRefused)) throw error;
      }
    })();
  });

  return overlay("dialog", {
    head: [head],
    body: [what(row, property), refusal],
    foot: [acts.row],
  }, close);
}

/** What withdrawing does to the balance — which depends on what was done to it. */
function what(row: LeaveRow, property: PropertyEnvironment): HTMLElement {
  const note = el("div", "note");
  const many = formatNumber(row.days, property);

  if (row.state === "Approved") {
    note.append(
      el("span", undefined, "It was approved, so the "),
      el("b", undefined, `${many} days`),
      el("span", undefined,
        " go back to your balance. The rota is not changed by this — whoever "
        + "plans it will see you are available again."));
  } else {
    // **Nothing is held while it waits, and the note must not imply it is.**
    // `LeaveService.ApproveAsync` debits at approval and says why in as many
    // words: *"debiting on request would let an undecided request hide capacity
    // from everybody else"*. A sentence about days being held would describe a
    // mechanism this service deliberately does not have.
    note.append(
      el("span", undefined, "It is still waiting, so nothing has been taken from your "),
      el("b", undefined, "balance"),
      el("span", undefined,
        ` — the ${many} days come off it only if it is approved. Withdrawing `
        + "takes it off your approver's queue, and you can ask again."));
  }

  return note;
}
